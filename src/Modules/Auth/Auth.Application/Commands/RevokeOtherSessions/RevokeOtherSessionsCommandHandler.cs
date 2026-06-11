using Auth.Application.Caching;
using Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.RevokeOtherSessions;

public sealed class RevokeOtherSessionsCommandHandler(
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache)
    : ICommandHandler<RevokeOtherSessionsCommand, RevokeOtherSessionsResult>
{
    public async Task<Result<RevokeOtherSessionsResult>> Handle(
        RevokeOtherSessionsCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<RevokeOtherSessionsResult>.Unauthorized("Authentication is required.");

        var userId = currentUser.UserId.Value;
        var currentSessionId = request.CurrentSessionId;

        var otherSessions = await sessionRepository.GetAllAsync(
            filter: s => s.UserId == userId && !s.IsRevoked && s.Id != currentSessionId,
            asNoTracking: false,
            ct: cancellationToken);

        if (otherSessions.Count == 0)
            return Result<RevokeOtherSessionsResult>.Success(new RevokeOtherSessionsResult(0));

        var otherSessionIds = otherSessions.Select(s => s.Id).ToHashSet();

        var activeRefreshTokens = await refreshTokenRepository.GetAllAsync(
            filter: rt => rt.UserId == userId && !rt.IsRevoked && rt.SessionId != currentSessionId,
            asNoTracking: false,
            ct: cancellationToken);

        foreach (var session in otherSessions)
            session.Revoke();

        foreach (var refreshToken in activeRefreshTokens)
        {
            if (otherSessionIds.Contains(refreshToken.SessionId))
                refreshToken.Revoke();
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<RevokeOtherSessionsResult>.Failure(
                new Error("Session.ConcurrencyConflict", "One or more sessions were modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(AuthCacheKeys.UserSessionsTag(userId), cancellationToken);

        return Result<RevokeOtherSessionsResult>.Success(
            new RevokeOtherSessionsResult(otherSessions.Count));
    }
}
