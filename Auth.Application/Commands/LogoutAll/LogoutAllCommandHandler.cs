using Auth.Application.Caching;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.LogoutAll;

public sealed class LogoutAllCommandHandler(
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache)
    : ICommandHandler<LogoutAllCommand>
{
    public async Task<Result> Handle(LogoutAllCommand request, CancellationToken canclellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Unauthorized("Authentication is required.");

        var userId = currentUser.UserId.Value;

        var activeSessions = await sessionRepository.GetAllAsync(
            filter: s => s.UserId == userId && !s.IsRevoked,
            asNoTracking: false,
            ct: canclellationToken);

        var activeRefreshTokens = await refreshTokenRepository.GetAllAsync(
            filter: rt => rt.UserId == userId && !rt.IsRevoked,
            asNoTracking: false,
            ct: canclellationToken);

        foreach (var session in activeSessions)
            session.Revoke();

        foreach (var refreshToken in activeRefreshTokens)
            refreshToken.Revoke();

        await unitOfWork.SaveChangesAsync(canclellationToken);

        await cache.RemoveByTagAsync(AuthCacheKeys.UserSessionsTag(userId), canclellationToken);

        return Result.Success();
    }
}
