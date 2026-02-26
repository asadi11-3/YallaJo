using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ISessionRepository sessionRepository,
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
        CancellationToken ct)
    {
        // 1. Hash the incoming plain token
        var hash = tokenService.HashRefreshToken(request.RefreshToken);

        // 2. Find active refresh token by hash
        var oldRefreshToken = await refreshTokenRepository.FirstOrDefaultAsync(
            filter: rt => rt.TokenHash == hash && !rt.IsRevoked && !rt.IsDeleted,
            asNoTracking: false,
            ct: ct);

        if (oldRefreshToken is null)
            return _invalidToken;

        // 3. Check revocation
        if (oldRefreshToken.IsRevoked)
            return Result<RefreshTokenResult>.Failure(
                Error.Unauthorized("Refresh token has been revoked."),
                Outcome.Unauthorized);

        // 4. Check expiration
        if (oldRefreshToken.ExpiresAt < DateTime.UtcNow)
            return Result<RefreshTokenResult>.Failure(
                Error.Unauthorized("Refresh token has expired."),
                Outcome.Unauthorized);

        // 5. Load the associated session
        var session = await sessionRepository.GetByIdAsync(
            oldRefreshToken.SessionId,
            ct: ct,
            asNoTracking: false);

        if (session is null || session.IsRevoked)
            return Result<RefreshTokenResult>.Failure(
                Error.Unauthorized("Session is no longer valid."),
                Outcome.Unauthorized);

        // 6. Load user data from Security module
        var userData = await securityService.GetUserDataByIdAsync(oldRefreshToken.UserId, ct);
        if (userData is null)
            return Result<RefreshTokenResult>.Failure(
                Error.Unauthorized("User account not found or deactivated."),
                Outcome.Unauthorized);

        // 7. ROTATION — Generate new refresh token
        var newPlainRefreshToken = tokenService.GenerateRefreshToken();
        var newHash = tokenService.HashRefreshToken(newPlainRefreshToken);
        var newExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays);

        var newRefreshToken = Domain.Entities.RefreshToken.Create(
            userId: oldRefreshToken.UserId,
            sessionId: oldRefreshToken.SessionId,
            tokenHash: newHash,
            expiresAt: newExpiresAt);
        await refreshTokenRepository.AddAsync(newRefreshToken, ct);

        // 8. Revoke old token, linking to the new one
        oldRefreshToken.Revoke(replacedByTokenId: newRefreshToken.Id);

        // 9. Update session timestamp
        session.MarkUpdated();

        // 10. Persist
        await unitOfWork.SaveChangesAsync(ct);

        // 11. Generate new access token
        var accessToken = tokenService.GenerateAccessToken(new TokenData(
            UserId: userData.UserId,
            Email: userData.Email,
            Roles: userData.Roles,
            AdditionalClaims: userData.Claims));

        return Result<RefreshTokenResult>.Success(new RefreshTokenResult(
            AccessToken: accessToken,
            RefreshToken: newPlainRefreshToken,
            RefreshTokenExpiresAt: newExpiresAt));
    }
}
