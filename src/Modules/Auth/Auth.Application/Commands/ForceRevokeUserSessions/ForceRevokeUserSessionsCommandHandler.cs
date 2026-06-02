using Auth.Application.Caching;
using Auth.Application.Errors;
using Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ForceRevokeUserSessions;

public sealed class ForceRevokeUserSessionsCommandHandler(
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthUnitOfWork unitOfWork,
    ISecurityService securityService,
    ICurrentUser currentUser,
    HybridCache cache)
    : ICommandHandler<ForceRevokeUserSessionsCommand>
{
    public async Task<Result> Handle(
        ForceRevokeUserSessionsCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure(AuthErrors.AdminUnauthenticated, Outcome.Unauthorized);
        }

        var actorId = currentUser.UserId.Value;
        var targetUserId = request.UserId;

        var guard = await securityService.EnsureCanManageUserAsync(
            actorId, targetUserId, cancellationToken);

        if (guard.IsFailure)
            return guard;

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

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("Session.ConcurrencyConflict", "One or more sessions were modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(AuthCacheKeys.UserSessionsTag(targetUserId), cancellationToken);

        return Result.Success();
    }
}
