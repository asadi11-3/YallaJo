using Auth.Application.Commands.Login;
using Auth.Application.ExternalAuth;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using RefreshTokenEntity = Auth.Domain.Entities.RefreshToken;

namespace Auth.Application.Commands.ExternalLogin;

/// <summary>
/// Signs the user in using a previously-linked external provider.
///
/// <para>Security properties:</para>
/// <list type="bullet">
///   <item><description>Accepts ONLY an HMAC-signed, single-use ticket produced
///   by the trusted Web BFF. Raw provider IDs are never accepted from clients.
///   </description></item>
///   <item><description>Ticket nonce consumed atomically before any session
///   creation — blocks replay of a captured ticket.</description></item>
///   <item><description>No auto-provisioning. If the provider identity is not
///   already linked to an active user, login is refused. Linking must be done
///   explicitly while authenticated, so that the server never has to trust a
///   provider-asserted email as proof of account ownership.</description></item>
///   <item><description>Deactivated/deleted users are rejected.</description></item>
///   <item><description>Email verification on the Security user is still
///   required — provider-asserted email is not a substitute for the user's
///   verified primary email on this platform.</description></item>
///   <item><description>Produces a Device + Session + RefreshToken using the
///   exact same pipeline as password login — identical trust-device / revoke /
///   refresh semantics apply.</description></item>
/// </list>
/// </summary>
public sealed class ExternalLoginCommandHandler(
    IExternalProviderRepository externalProviderRepository,
    IDeviceRepository deviceRepository,
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthUnitOfWork unitOfWork,
    ISecurityService securityService,
    ITokenService tokenService,
    IExternalAuthTicketVerifier ticketVerifier,
    IExternalAuthNonceStore nonceStore,
    IRequestContext requestContext,
    ILogger<ExternalLoginCommandHandler> logger)
    : ICommandHandler<ExternalLoginCommand, LoginResult>
{
    private const int SessionDays = 30;
    private const int RefreshTokenDays = 30;

    private static readonly Result<LoginResult> _invalidTicket =
        Result<LoginResult>.Failure(
            Error.Unauthorized("Invalid or expired external-provider sign-in."),
            Outcome.Unauthorized);

    public async Task<Result<LoginResult>> Handle(ExternalLoginCommand request, CancellationToken ct)
    {
        // 1. Verify the signed ticket.
        var verification = ticketVerifier.Verify(request.Ticket);
        if (verification.IsFailure)
        {
            logger.LogInformation(
                "External login rejected: invalid ticket ({Error}).",
                verification.Error?.Message);
            return _invalidTicket;
        }

        var ticket = verification.Value!;
        var normalizedProvider = ticket.Provider.Trim().ToLowerInvariant();

        // 2. Consume the ticket nonce ONCE.
        var consumed = await nonceStore.TryConsumeAsync(ticket.TicketId, ticket.ExpiresAt, ct);
        if (!consumed)
        {
            logger.LogWarning(
                "External login replay detected: ticket {TicketId} already consumed.",
                ticket.TicketId);
            return _invalidTicket;
        }

        // 3. Find the active link. No link = refuse login (no auto-provision).
        var link = await externalProviderRepository.FindActiveLinkAsync(
            normalizedProvider, ticket.ProviderUserId, ct);

        if (link is null)
        {
            // Deliberately generic message so that we don't reveal whether a
            // given Google/Facebook account is linked to any user on this site.
            return _invalidTicket;
        }

        // 4. Load the Security-side user data (roles / claims / email verified).
        var userData = await securityService.GetUserDataByIdAsync(link.UserId, ct);
        if (userData is null)
        {
            // User deleted / deactivated after linking.
            logger.LogWarning(
                "External login for provider {Provider} user {ProviderUserId} blocked: Security user {UserId} missing or inactive.",
                normalizedProvider, ticket.ProviderUserId, link.UserId);
            return _invalidTicket;
        }

        if (!userData.IsEmailVerified)
        {
            return Result<LoginResult>.Failure(
                Error.Unauthorized("Email not verified. Please verify your email before signing in."),
                Outcome.Unauthorized);
        }

        // 5. Create Device → Session → RefreshToken — identical pipeline to
        //    password login. This keeps trust-device, revoke and refresh
        //    semantics uniform across all login methods.
        var device = Device.Create(
            userId: userData.UserId,
            deviceToken: Guid.CreateVersion7().ToString(),
            userAgent: requestContext.UserAgent,
            deviceName: requestContext.DeviceName);
        await deviceRepository.AddAsync(device, ct);

        var session = Session.Create(
            userId: userData.UserId,
            deviceId: device.Id,
            expiresAt: DateTime.UtcNow.AddDays(SessionDays),
            ipAddress: requestContext.IpAddress);
        await sessionRepository.AddAsync(session, ct);

        var plainRefreshToken = tokenService.GenerateRefreshToken();
        var refreshTokenHash = tokenService.HashRefreshToken(plainRefreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays);

        var refreshToken = RefreshTokenEntity.Create(
            userId: userData.UserId,
            sessionId: session.Id,
            tokenHash: refreshTokenHash,
            expiresAt: refreshTokenExpiresAt);
        await refreshTokenRepository.AddAsync(refreshToken, ct);

        await unitOfWork.SaveChangesAsync(ct);

        var accessToken = tokenService.GenerateAccessToken(new TokenData(
            UserId: userData.UserId,
            Email: userData.Email,
            Roles: userData.Roles,
            AdditionalClaims: userData.Claims,
            SessionId: session.Id));

        return Result<LoginResult>.Success(new LoginResult(
            UserId: userData.UserId,
            AccessToken: accessToken,
            RefreshToken: plainRefreshToken,
            RefreshTokenExpiresAt: refreshTokenExpiresAt));
    }
}
