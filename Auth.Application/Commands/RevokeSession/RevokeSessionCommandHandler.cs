// Auth.Application/Commands/RevokeSession/RevokeSessionCommandHandler.cs
using Auth.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.RevokeSession;

public sealed class RevokeSessionCommandHandler(
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<RevokeSessionCommand>
{
    public async Task<Result> Handle(
        RevokeSessionCommand request,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Unauthorized("Authentication is required.");

        var userId = currentUser.UserId.Value;

        // 1. Load session (tracked — will be mutated)
        var session = await sessionRepository.GetByIdAsync(
            request.SessionId,
            ct: ct,
            asNoTracking: false);

        if (session is null)
            return Result.NotFound($"Session {request.SessionId} was not found.");

        // 2. SECURITY: caller may only revoke their own sessions
        if (session.UserId != userId)
            return Result.Forbidden("You are not authorized to revoke this session.");

        // 3. Revoke session (idempotent — already-revoked is still OK)
        if (!session.IsRevoked)
            session.Revoke();

        // 4. Revoke all active refresh tokens for this session
        var activeTokens = await refreshTokenRepository.GetAllAsync(
            filter: rt => rt.SessionId == request.SessionId && !rt.IsRevoked,
            asNoTracking: false,
            ct: ct);

        foreach (var token in activeTokens)
            token.Revoke();

        // 5. Persist
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
