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
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Unauthorized("Authentication is required.");

        var userId = currentUser.UserId.Value;
        var session = await sessionRepository.GetByIdAsync(
            request.SessionId,
            ct: cancellationToken,
            asNoTracking: false);

        if (session is null)
            return Result.NotFound($"Session {request.SessionId} was not found.");
        if (session.UserId != userId)
            return Result.Forbidden("You are not authorized to revoke this session.");

        if (!session.IsRevoked)
            session.Revoke();
        var activeTokens = await refreshTokenRepository.GetAllAsync(
            filter: rt => rt.SessionId == request.SessionId && !rt.IsRevoked,
            asNoTracking: false,
            ct: cancellationToken);

        foreach (var token in activeTokens)
            token.Revoke();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
