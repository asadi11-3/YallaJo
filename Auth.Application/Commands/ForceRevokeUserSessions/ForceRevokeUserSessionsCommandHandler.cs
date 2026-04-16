using Auth.Application.Caching;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ForceRevokeUserSessions;

public sealed class ForceRevokeUserSessionsCommandHandler(
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<ForceRevokeUserSessionsCommand>
{
    public async Task<Result> Handle(
        ForceRevokeUserSessionsCommand request,
        CancellationToken cancellationToken)
    {
        // Authentication and permission (UpdateAny) are enforced by the endpoint.
        // This handler operates on the target user supplied in the command, not the caller.
        var targetUserId = request.UserId;

        var activeSessions = await sessionRepository.GetAllAsync(
            filter: s => s.UserId == targetUserId && !s.IsRevoked,
            asNoTracking: false,
            ct: cancellationToken);

        var activeRefreshTokens = await refreshTokenRepository.GetAllAsync(
            filter: rt => rt.UserId == targetUserId && !rt.IsRevoked,
            asNoTracking: false,
            ct: cancellationToken);

        foreach (var session in activeSessions)
            session.Revoke();

        foreach (var refreshToken in activeRefreshTokens)
            refreshToken.Revoke();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(AuthCacheKeys.UserSessionsTag(targetUserId), cancellationToken);

        return Result.Success();
    }
}
