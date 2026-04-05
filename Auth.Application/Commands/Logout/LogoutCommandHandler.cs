using Auth.Application.Caching;
using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.Logout;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ISessionRepository sessionRepository,
    IAuthUnitOfWork unitOfWork,
    ITokenService tokenService,
    HybridCache cache)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        var hash = tokenService.HashRefreshToken(request.RefreshToken);

        var refreshToken = await refreshTokenRepository.FirstOrDefaultAsync(
            filter: rt => rt.TokenHash == hash,
            asNoTracking: false,
            ct: ct);

        if (refreshToken is null)
            return Result.Success(); // idempotent — token not found is still a successful logout

        var session = await sessionRepository.GetByIdAsync(
            refreshToken.SessionId, ct: ct, asNoTracking: false);

        if (!refreshToken.IsRevoked)
            refreshToken.Revoke();

        if (session is not null && !session.IsRevoked)
            session.Revoke();

        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(AuthCacheKeys.UserSessionsTag(refreshToken.UserId), ct);

        return Result.Success();
    }
}
