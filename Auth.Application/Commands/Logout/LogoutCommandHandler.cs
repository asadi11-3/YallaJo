using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.Logout;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ISessionRepository sessionRepository,
    IAuthUnitOfWork unitOfWork,
    ITokenService tokenService)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(
        LogoutCommand request,
        CancellationToken ct)
    {
        // 1. Hash the incoming plain token
        var hash = tokenService.HashRefreshToken(request.RefreshToken);

        // 2. Find refresh token by hash (may already be revoked — that's fine)
        var refreshToken = await refreshTokenRepository.FirstOrDefaultAsync(
            filter: rt => rt.TokenHash == hash,
            asNoTracking: false,
            ct: ct);

        if (refreshToken is null)
            return Result.Success(); // idempotent — token not found is still a successful logout

        // 3. Load associated session
        var session = await sessionRepository.GetByIdAsync(
            refreshToken.SessionId,
            ct: ct,
            asNoTracking: false);

        // 4. Revoke refresh token
        if (!refreshToken.IsRevoked)
            refreshToken.Revoke();

        // 5. Revoke session
        if (session is not null && !session.IsRevoked)
            session.Revoke();

        // 6. Persist
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
