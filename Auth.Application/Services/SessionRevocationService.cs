using Auth.Application.Interfaces.SessionRevocation;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Services;

internal sealed class SessionRevocationService(
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ILogger<SessionRevocationService> logger)
    : ISessionRevocationService
{
    public async Task<SessionRevocationOutcome> RevokeAllForUserAsync(
        Guid userId,
        SessionRevocationReason reason,
        CancellationToken cancellationToken = default)
    {
        var sessionsRevoked      = await sessionRepository.RevokeAllActiveForUserAsync(userId, cancellationToken);
        var refreshTokensRevoked = await refreshTokenRepository.RevokeAllActiveForUserAsync(userId, cancellationToken);

        if (sessionsRevoked > 0 || refreshTokensRevoked > 0)
        {
            logger.LogInformation(
                "Auth: Revoked {SessionCount} session(s) and {RefreshTokenCount} refresh token(s) for user {UserId} due to {Reason}.",
                sessionsRevoked, refreshTokensRevoked, userId, reason);
        }

        return new SessionRevocationOutcome(sessionsRevoked, refreshTokensRevoked);
    }
}
