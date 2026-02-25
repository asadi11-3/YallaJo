using Auth.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.LogoutAll;

public sealed class LogoutAllCommandHandler(
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<LogoutAllCommand>
{
    public async Task<Result> Handle(
        LogoutAllCommand request,
        CancellationToken ct)
    {
        // 1. Ensure user is authenticated
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Unauthorized("Authentication is required.");

        var userId = currentUser.UserId.Value;

        // 2. Get all active sessions for this user
        var activeSessions = await sessionRepository.GetAllAsync(
            filter: s => s.UserId == userId && !s.IsRevoked,
            asNoTracking: false,
            ct: ct);

        // 3. Get all active refresh tokens for this user
        var activeRefreshTokens = await refreshTokenRepository.GetAllAsync(
            filter: rt => rt.UserId == userId && !rt.IsRevoked,
            asNoTracking: false,
            ct: ct);

        // 4. Revoke all sessions
        foreach (var session in activeSessions)
            session.Revoke();

        // 5. Revoke all refresh tokens
        foreach (var refreshToken in activeRefreshTokens)
            refreshToken.Revoke();

        // 6. Persist
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
