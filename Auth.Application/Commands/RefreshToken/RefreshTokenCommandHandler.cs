using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ISessionRepository sessionRepository,
    IDeviceRepository deviceRepository,
    ISecurityService securityService,
    IAuthUnitOfWork unitOfWork,
    ITokenService tokenService)
    : ICommandHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private const int RefreshTokenDays = 30;

    private static readonly Result<RefreshTokenResult> _invalidToken =
        Result<RefreshTokenResult>.Failure(
            Error.Unauthorized("Invalid or expired refresh token."),
            Outcome.Unauthorized);

    public async Task<Result<RefreshTokenResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var hash = tokenService.HashRefreshToken(request.RefreshToken);

        var oldRefreshToken = await refreshTokenRepository.FirstOrDefaultAsync(
            filter: rt => rt.TokenHash == hash && !rt.IsDeleted,
            asNoTracking: false,
            ct: cancellationToken);

        if (oldRefreshToken is null)
            return _invalidToken;

        if (oldRefreshToken.IsRevoked)
        {
            var compromisedTokens = await refreshTokenRepository.GetAllAsync(
                filter: rt => rt.SessionId == oldRefreshToken.SessionId && !rt.IsRevoked,
                asNoTracking: false,
                ct: cancellationToken);

            var compromisedSession = await sessionRepository.GetByIdAsync(
                oldRefreshToken.SessionId,
                ct: cancellationToken,
                asNoTracking: false);

            foreach (var t in compromisedTokens)
                t.Revoke();

            if (compromisedSession is not null && !compromisedSession.IsRevoked)
                compromisedSession.Revoke();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<RefreshTokenResult>.Failure(
                Error.Unauthorized(
                    "Token reuse detected. For your security, this session has been terminated."),
                Outcome.Unauthorized);
        }

        if (oldRefreshToken.ExpiresAt < DateTime.UtcNow)
            return _invalidToken;

        var activeSession = await sessionRepository.GetByIdAsync(
            oldRefreshToken.SessionId,
            ct: cancellationToken,
            asNoTracking: false);

        if (activeSession is null || activeSession.IsRevoked)
        {
            return Result<RefreshTokenResult>.Failure(
                Error.Unauthorized("Session is no longer valid."),
                Outcome.Unauthorized);
        }

        var userData = await securityService.GetUserDataByIdAsync(
            oldRefreshToken.UserId, cancellationToken);

        if (userData is null)
        {
            return Result<RefreshTokenResult>.Failure(
                Error.Unauthorized("User account not found or deactivated."),
                Outcome.Unauthorized);
        }

       
        if (!userData.IsEmailVerified)
        {
            return _invalidToken;
        }

        if (userData.Lifecycle != AccountLifecycleSnapshot.Active)
        {
            return _invalidToken;
        }

        // ── Token rotation ──────────────────────────────────────────────────
        var newPlainRefreshToken = tokenService.GenerateRefreshToken();
        var newHash              = tokenService.HashRefreshToken(newPlainRefreshToken);
        var newExpiresAt         = DateTime.UtcNow.AddDays(RefreshTokenDays);

        var newRefreshToken = Domain.Entities.RefreshToken.Create(
            userId:    oldRefreshToken.UserId,
            sessionId: oldRefreshToken.SessionId,
            tokenHash: newHash,
            expiresAt: newExpiresAt);
        await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        oldRefreshToken.Revoke(replacedByTokenId: newRefreshToken.Id);

        var device = await deviceRepository.GetByIdAsync(
            activeSession.DeviceId, ct: cancellationToken, asNoTracking: false);
        device?.RecordSeen();
        activeSession.MarkUpdated();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(new TokenData(
            UserId:           userData.UserId,
            Email:            userData.Email,
            Roles:            userData.Roles,
            AdditionalClaims: userData.Claims,
            SessionId:        oldRefreshToken.SessionId));

        return Result<RefreshTokenResult>.Success(new RefreshTokenResult(
            AccessToken:           accessToken,
            RefreshToken:          newPlainRefreshToken,
            RefreshTokenExpiresAt: newExpiresAt));
    }
}
