using Auth.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ForceRevokeUserSessions;

public sealed class ForceRevokeUserSessionsCommandHandler(
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<ForceRevokeUserSessionsCommand>
{
    public async Task<Result> Handle(
        ForceRevokeUserSessionsCommand request,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Result.Unauthorized("Authentication is required.");

        var targetUserId = request.UserId;

        // 1. Revoke all active sessions for the target user
        var activeSessions = await sessionRepository.GetAllAsync(
            filter: s => s.UserId == targetUserId && !s.IsRevoked,
            asNoTracking: false,
            ct: ct);

        // 2. Revoke all active refresh tokens for the target user
        var activeRefreshTokens = await refreshTokenRepository.GetAllAsync(
            filter: rt => rt.UserId == targetUserId && !rt.IsRevoked,
            asNoTracking: false,
            ct: ct);

        foreach (var session in activeSessions)
            session.Revoke();

        foreach (var refreshToken in activeRefreshTokens)
            refreshToken.Revoke();

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
