using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using RefreshTokenEntity = Auth.Domain.Entities.RefreshToken;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    ISecurityService securityService,
    IOtpRepository otpRepository,
    IDeviceRepository deviceRepository,
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    ITokenService tokenService,
    IRequestContext requestContext)
    : ICommandHandler<VerifyEmailCommand, VerifyEmailResult>
{
    private const int MaxOtpAttempts = 5;
    private const int SessionDays = 30;
    private const int RefreshTokenDays = 30;

    public async Task<Result<VerifyEmailResult>> Handle(
        VerifyEmailCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Resolve UserId from Security module
        var userId = await securityService.GetUserIdByEmailAsync(normalizedEmail, ct);
        if (userId is null)
            return Result<VerifyEmailResult>.Failure(
                Error.NotFound("User.NotFound", "No account found with this email."),
                Outcome.NotFound);

        // 2. Get latest active OTP for email verification
        var otp = await otpRepository.FirstOrDefaultAsync(
            filter: o => o.UserId == userId.Value
                      && o.Purpose == "EmailVerification"
                      && !o.IsUsed,
            orderBy: q => q.OrderByDescending(o => o.CreatedAt),
            asNoTracking: false,
            ct: ct);

        if (otp is null)
            return Result<VerifyEmailResult>.Failure(
                Error.NotFound("Otp.NotFound", "No pending verification code found. Please register again."),
                Outcome.NotFound);

        // 3. Check brute force protection
        if (otp.AttemptCount >= MaxOtpAttempts)
            return Result<VerifyEmailResult>.Fail(
                Outcome.TooManyRequests,
                "Too many verification attempts. Please request a new code.");

        // 4. Check expiration
        if (otp.ExpiresAt < DateTime.UtcNow)
            return Result<VerifyEmailResult>.Failure(
                Error.Validation("Otp.Expired", "Verification code has expired. Please request a new one."),
                Outcome.Invalid);

        // 5. Record attempt and verify code
        otp.IncrementAttempt();

        if (!otpService.Verify(request.OtpCode, otp.CodeHash))
        {
            await unitOfWork.SaveChangesAsync(ct); // persist attempt count
            return Result<VerifyEmailResult>.Failure(
                Error.Validation("Otp.Invalid", "Invalid verification code."),
                Outcome.Invalid);
        }

        // 6. OTP is valid — mark as used
        otp.MarkUsed();

        // 7. Mark email verified in Security module (activates user + raises EmailVerifiedEvent)
        var verified = await securityService.MarkEmailVerifiedAsync(userId.Value, normalizedEmail, ct);
        if (!verified)
            return Result<VerifyEmailResult>.Failure(
                Error.Failure("Verification.Failed", "Could not verify email. Please try again."),
                Outcome.ServerError);

        // 8. Create Device
        var device = Device.Create(
            userId: userId.Value,
            deviceToken: Guid.NewGuid().ToString(),
            userAgent: requestContext.UserAgent,
            deviceName: requestContext.DeviceName);
        await deviceRepository.AddAsync(device, ct);

        // 9. Create Session
        var session = Session.Create(
            userId: userId.Value,
            deviceId: device.Id,
            expiresAt: DateTime.UtcNow.AddDays(SessionDays),
            ipAddress: requestContext.IpAddress);
        await sessionRepository.AddAsync(session, ct);

        // 10. Generate & store RefreshToken (hash only in DB)
        var plainRefreshToken = tokenService.GenerateRefreshToken();
        var refreshTokenHash = tokenService.HashRefreshToken(plainRefreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays);

        var refreshToken = RefreshTokenEntity.Create(
            userId: userId.Value,
            sessionId: session.Id,
            tokenHash: refreshTokenHash,
            expiresAt: refreshTokenExpiresAt);
        await refreshTokenRepository.AddAsync(refreshToken, ct);

        // 11. Persist all Auth entities
        await unitOfWork.SaveChangesAsync(ct);

        // 12. Generate JWT AccessToken — load real roles/claims from Security module
        var userData = await securityService.GetUserDataByIdAsync(userId.Value, ct);
        var accessToken = tokenService.GenerateAccessToken(new TokenData(
            UserId: userId.Value,
            Email: normalizedEmail,
            Roles: userData?.Roles ?? [],
            AdditionalClaims: userData?.Claims ?? [],
            SessionId: session.Id));

        return Result<VerifyEmailResult>.Success(new VerifyEmailResult(
            UserId: userId.Value,
            AccessToken: accessToken,
            RefreshToken: plainRefreshToken,
            RefreshTokenExpiresAt: refreshTokenExpiresAt));
    }
}
