using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ISessionRepository sessionRepository,
    IDeviceRepository deviceRepository,
    ISecurityService securityService,
    IAuthUnitOfWork unitOfWork,
    ITokenService tokenService)
    : ICommandHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private const int RefreshTokenDays = 30;

    private static readonly Result<RefreshTokenResult> _invalidToken =
        Result<RefreshTokenResult>.Failure(
            Error.Unauthorized("Invalid or expired refresh token."),
            Outcome.Unauthorized);

    public async Task<Result<RefreshTokenResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var oldRefreshToken = await refreshTokenRepository.FirstOrDefaultAsync(
            filter: rt => rt.TokenHash == hash && !rt.IsRevoked && !rt.IsDeleted,
            asNoTracking: false,
            ct: cancellationToken);

        if (oldRefreshToken is null)
            return _invalidToken;

        if (oldRefreshToken.IsRevoked)
        {
            return Result<RefreshTokenResult>.Failure(
                    Error.Unauthorized("Refresh token has been revoked."),
                    Outcome.Unauthorized);
        }

        if (oldRefreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return Result<RefreshTokenResult>.Failure(
                  Error.Unauthorized("Refresh token has expired."),
                  Outcome.Unauthorized);
        }

        var session = await sessionRepository.GetByIdAsync(
            oldRefreshToken.SessionId,
            ct: cancellationToken,
            asNoTracking: false);

        if (session is null || session.IsRevoked)
        {
            return Result<RefreshTokenResult>.Failure(
                  Error.Unauthorized("Session is no longer valid."),
                  Outcome.Unauthorized);
        }

        var userData = await securityService.GetUserDataByIdAsync(oldRefreshToken.UserId, cancellationToken);
        if (userData is null) {
            return Result<RefreshTokenResult>.Failure(
              Error.Unauthorized("User account not found or deactivated."),
              Outcome.Unauthorized);
        }

        var newPlainRefreshToken = tokenService.GenerateRefreshToken();
        var newHash = tokenService.HashRefreshToken(newPlainRefreshToken);
        var newExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays);

        var newRefreshToken = Domain.Entities.RefreshToken.Create(
            userId: oldRefreshToken.UserId,
            sessionId: oldRefreshToken.SessionId,
            tokenHash: newHash,
            expiresAt: newExpiresAt);
        await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        oldRefreshToken.Revoke(replacedByTokenId: newRefreshToken.Id);
        var device = await deviceRepository.GetByIdAsync(session.DeviceId, ct: cancellationToken, asNoTracking: false);
        device?.RecordSeen();
        session.MarkUpdated();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var accessToken = tokenService.GenerateAccessToken(new TokenData(
            UserId: userData.UserId,
            Email: userData.Email,
            Roles: userData.Roles,
            AdditionalClaims: userData.Claims,
            SessionId: oldRefreshToken.SessionId));

        return Result<RefreshTokenResult>.Success(new RefreshTokenResult(
            AccessToken: accessToken,
            RefreshToken: newPlainRefreshToken,
            RefreshTokenExpiresAt: newExpiresAt));
    }
}
