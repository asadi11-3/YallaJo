using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using RefreshTokenEntity = Auth.Domain.Entities.RefreshToken;

namespace Auth.Application.Commands.Login;

public sealed class LoginCommandHandler(
    ISecurityService securityService,
    IDeviceRepository deviceRepository,
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthUnitOfWork unitOfWork,
    ITokenService tokenService,
    IRequestContext requestContext)
    : ICommandHandler<LoginCommand, LoginResult>
{
    private const int SessionDays = 30;
    private const int RefreshTokenDays = 30;

    private static readonly Result<LoginResult> _invalidCredentials =
        Result<LoginResult>.Failure(
            Error.Unauthorized("Invalid email or password."),
            Outcome.Unauthorized);

    public async Task<Result<LoginResult>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Verify credentials via Security module (password check + get roles/claims)
        var userData = await securityService.VerifyCredentialsAsync(normalizedEmail, request.Password, cancellationToken);
        if (userData is null)
            return _invalidCredentials;

        // 2. Check email verification
        if (!userData.IsEmailVerified)
        {
            return Result<LoginResult>.Failure(
                    Error.Unauthorized("Email not verified. Please verify your email first."),
                    Outcome.Unauthorized);
        }

        // 3. Lifecycle gate (Phase 2A) — login is permitted ONLY when the
        //    account is in the Active state. PendingActivation, Suspended,
        //    PendingPasswordReset, and Archived all reject regardless of
        //    credential validity. Email-verified-but-not-Active is treated
        //    the same way as invalid credentials from the user's perspective
        //    (no information leak), but logged as a distinct denial reason.
        if (userData.Lifecycle != AccountLifecycleSnapshot.Active)
        {
            return Result<LoginResult>.Failure(
                Error.Unauthorized("This account is not currently active. Contact your administrator."),
                Outcome.Unauthorized);
        }

        // 4. Create Device
        var device = Device.Create(
            userId:      userData.UserId,
            deviceToken: Guid.CreateVersion7().ToString(),
            userAgent:   requestContext.UserAgent,
            deviceName:  requestContext.DeviceName);
        await deviceRepository.AddAsync(device, cancellationToken);

        // 5. Create Session
        var session = Session.Create(
            userId: userData.UserId,
            deviceId: device.Id,
            expiresAt: DateTime.UtcNow.AddDays(SessionDays),
            ipAddress: requestContext.IpAddress);
        await sessionRepository.AddAsync(session, cancellationToken);

        // 6. Generate & store RefreshToken (hash only in DB)
        var plainRefreshToken = tokenService.GenerateRefreshToken();
        var refreshTokenHash = tokenService.HashRefreshToken(plainRefreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays);

        var refreshToken = RefreshTokenEntity.Create(
            userId: userData.UserId,
            sessionId: session.Id,
            tokenHash: refreshTokenHash,
            expiresAt: refreshTokenExpiresAt);
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        // 7. Persist all Auth entities
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 8. Generate JWT AccessToken
        var accessToken = tokenService.GenerateAccessToken(new TokenData(
            UserId: userData.UserId,
            Email: userData.Email,
            Roles: userData.Roles,
            AdditionalClaims: userData.Claims,
            SessionId: session.Id));

        return Result<LoginResult>.Success(new LoginResult(
            UserId: userData.UserId,
            AccessToken: accessToken,
            RefreshToken: plainRefreshToken,
            RefreshTokenExpiresAt: refreshTokenExpiresAt));
    }
}
