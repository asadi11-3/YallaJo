using Accounts.Contracts.Abstractions;
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
/// Signs the user in using an external provider (Google, Facebook, …).
///
/// <para>Three outcomes are possible, all short-circuited to a single generic
/// failure if any safety gate trips (fail-closed; no oracle leak):</para>
///
/// <list type="number">
///   <item><description><b>Existing link</b> — (provider, providerUserId) is
///   already mapped to an active local user → just sign them in.</description></item>
///   <item><description><b>Auto-link</b> — no link exists, but the provider-
///   asserted email matches a local account whose primary email is verified →
///   attach the new link to that user and sign in.</description></item>
///   <item><description><b>Auto-create</b> — no link AND no local account with
///   this email → provision a new local identity (Security user + Accounts
///   profile) seeded from the provider's verified email / given / family
///   name, attach the link, and sign in.</description></item>
/// </list>
///
/// <para>Security invariants for auto-link and auto-create:</para>
/// <list type="bullet">
///   <item><description>Ticket is HMAC-signed and single-use (nonce consumed
///   atomically before any state mutation).</description></item>
///   <item><description>Ticket MUST carry an email AND
///   <c>EmailVerifiedByProvider = true</c>. Facebook has no native flag — the
///   Web BFF synthesizes it when Meta surfaces an email, because Meta only
///   releases the email after user confirmation.</description></item>
///   <item><description>Auto-link requires the local account's primary email
///   to also be verified on THIS platform — the provider-asserted email is
///   not a substitute for the user's own verification on our side.</description></item>
///   <item><description>Auto-create NEVER overrides an existing local account
///   — <c>RegisterExternalAsync</c> returns Conflict if the email is taken,
///   which is treated as the concurrent race that another caller already
///   auto-linked/created, and the flow retries via the existing-link path.</description></item>
///   <item><description>The provider identity (provider + providerUserId) is
///   re-checked against a potential conflict with another user both before
///   the link write and enforced by the database's filtered unique index.</description></item>
/// </list>
///
/// <para>All refusals log a SINGLE structured Warning line containing
/// <c>Reason=&lt;AutoLinkRefusalReason&gt;</c> so operators can see WHY the
/// client got the generic error without being able to leak that reason to the
/// client response.</para>
/// </summary>
public sealed class ExternalLoginCommandHandler(
    IExternalProviderRepository externalProviderRepository,
    IDeviceRepository deviceRepository,
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthUnitOfWork unitOfWork,
    ISecurityService securityService,
    IUserRegistrationService userRegistrationService,
    IProfileCreationService profileCreationService,
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
        // 1. Verify signed ticket.
        var verification = ticketVerifier.Verify(request.Ticket);
        if (verification.IsFailure)
        {
            logger.LogWarning(
                "External login rejected: invalid ticket ({Error}).",
                verification.Error?.Message);
            return _invalidTicket;
        }

        var ticket = verification.Value!;
        var normalizedProvider = ticket.Provider.Trim().ToLowerInvariant();

        logger.LogInformation(
            "External login attempt: Provider={Provider} ProviderUserId={ProviderUserId} HasEmail={HasEmail} EmailVerifiedByProvider={EmailVerifiedByProvider} TicketId={TicketId}",
            normalizedProvider,
            ticket.ProviderUserId,
            !string.IsNullOrWhiteSpace(ticket.Email),
            ticket.EmailVerifiedByProvider,
            ticket.TicketId);

        // 2. Consume nonce ONCE.
        var consumed = await nonceStore.TryConsumeAsync(ticket.TicketId, ticket.ExpiresAt, ct);
        if (!consumed)
        {
            logger.LogWarning(
                "External login replay detected: ticket {TicketId} already consumed.",
                ticket.TicketId);
            return _invalidTicket;
        }

        // 3. Resolve or provision the owning user.
        var resolve = await ResolveOrProvisionUserAsync(ticket, normalizedProvider, ct);
        if (resolve.Reason != AutoLinkRefusalReason.None)
        {
            logger.LogWarning(
                "External login refused: Reason={Reason} Path={Path} Provider={Provider} ProviderUserId={ProviderUserId} TicketEmail={TicketEmail} ProviderAssertsVerified={ProviderAssertsVerified} CandidateUserId={CandidateUserId} LocalEmailVerified={LocalEmailVerified}",
                resolve.Reason,
                resolve.Path,
                normalizedProvider,
                ticket.ProviderUserId,
                ticket.Email ?? "(null)",
                ticket.EmailVerifiedByProvider,
                resolve.CandidateUserId?.ToString() ?? "(none)",
                resolve.LocalEmailVerified);
            return _invalidTicket;
        }

        var userId = resolve.LinkedUserId!.Value;

        // 4. Load Security user data.
        var userData = await securityService.GetUserDataByIdAsync(userId, ct);
        if (userData is null)
        {
            logger.LogWarning(
                "External login for provider {Provider} user {ProviderUserId} blocked: Security user {UserId} missing or inactive.",
                normalizedProvider, ticket.ProviderUserId, userId);
            return _invalidTicket;
        }

        if (!userData.IsEmailVerified)
        {
            // Belt & braces — auto-create sets this to true and auto-link
            // refuses when it's false, so this only trips on a race or a
            // deliberately-unverified legacy account.
            logger.LogWarning(
                "External login blocked for user {UserId}: local primary email not verified on platform.",
                userId);
            return Result<LoginResult>.Failure(
                Error.Unauthorized("Email not verified. Please verify your email before signing in."),
                Outcome.Unauthorized);
        }

        // 5. Device → Session → RefreshToken (identical pipeline to password login).
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

    /// <summary>
    /// Three-way resolver: existing link → auto-link → auto-create. Returns
    /// an outcome carrying either the resolved <c>UserId</c> or the exact
    /// refusal reason + which path refused (for operator-visible logs). Fails
    /// closed.
    /// </summary>
    private async Task<AutoLinkOutcome> ResolveOrProvisionUserAsync(
        ExternalAuthTicket ticket,
        string normalizedProvider,
        CancellationToken ct)
    {
        // (1) Existing link wins.
        var link = await externalProviderRepository.FindActiveLinkAsync(
            normalizedProvider, ticket.ProviderUserId, ct);
        if (link is not null)
        {
            logger.LogInformation(
                "External login: existing link resolved for provider {Provider}, user {UserId}.",
                normalizedProvider, link.UserId);
            return AutoLinkOutcome.Linked(link.UserId, AutoLinkPath.ExistingLink);
        }

        // (2) Every un-linked path requires a provider-verified email.
        if (string.IsNullOrWhiteSpace(ticket.Email))
            return AutoLinkOutcome.Refused(AutoLinkRefusalReason.TicketMissingEmail, AutoLinkPath.Unresolved);

        if (!ticket.EmailVerifiedByProvider)
            return AutoLinkOutcome.Refused(AutoLinkRefusalReason.ProviderDidNotVerifyEmail, AutoLinkPath.Unresolved);

        var normalizedEmail = ticket.Email.Trim().ToLowerInvariant();

        // Resolve local account by email. Not-found = auto-create path;
        // found = auto-link path.
        var candidateUserId = await securityService.GetUserIdByEmailAsync(normalizedEmail, ct);
        if (candidateUserId is null)
        {
            // (3) AUTO-CREATE path.
            return await TryAutoCreateAsync(ticket, normalizedProvider, normalizedEmail, ct);
        }

        // (4) AUTO-LINK path.
        return await TryAutoLinkAsync(ticket, normalizedProvider, normalizedEmail, candidateUserId.Value, ct);
    }

    /// <summary>
    /// Scenario A: provision a brand-new local identity from the provider's
    /// verified claims, then link this provider to it. Fails closed on any
    /// conflict — if the email is already taken (race), we treat that as a
    /// signal that a concurrent request already created the account and we
    /// refuse rather than silently merging.
    /// </summary>
    private async Task<AutoLinkOutcome> TryAutoCreateAsync(
        ExternalAuthTicket ticket,
        string normalizedProvider,
        string normalizedEmail,
        CancellationToken ct)
    {
        // Defense in depth: the provider identity must not already be owned
        // by another user (the existing-link path above already checked, but
        // re-check to be race-tolerant).
        var takenByOther = await externalProviderRepository.AnyAsync(
            ep => ep.Provider == normalizedProvider
               && ep.ProviderUserId == ticket.ProviderUserId
               && ep.IsActive,
            ct);

        if (takenByOther)
        {
            return AutoLinkOutcome.Refused(
                AutoLinkRefusalReason.ProviderIdentityOwnedByAnotherUser,
                AutoLinkPath.AutoCreate);
        }

        // Provision Security user (primary email already verified, active).
        var registration = await userRegistrationService.RegisterExternalAsync(
            new ExternalUserRegistrationRequest(
                Email:     normalizedEmail,
                FirstName: ticket.FirstName ?? string.Empty,
                LastName:  ticket.LastName ?? string.Empty),
            ct);

        if (registration.IsFailure)
        {
            return AutoLinkOutcome.Refused(
                registration.Outcome == Outcome.Conflict
                    ? AutoLinkRefusalReason.ConcurrentAccountCreated
                    : AutoLinkRefusalReason.AutoCreateFailed,
                AutoLinkPath.AutoCreate);
        }

        var newUserId = registration.Value;

        // Provision Accounts profile. Accounts has its own DbContext — this
        // write commits independently. We tolerate Conflict (the profile
        // already exists, e.g. because of a retry) but treat any other
        // failure as a hard stop with loud logging, because the Security
        // user now exists without a profile.
        var profile = await profileCreationService.CreateForUserAsync(
            new ProfileCreationRequest(
                UserId:    newUserId,
                FirstName: ticket.FirstName ?? "User",
                LastName:  ticket.LastName ?? string.Empty),
            ct);

        if (profile.IsFailure && profile.Outcome != Outcome.Conflict)
        {
            logger.LogError(
                "Auto-create for provider {Provider} email {Email}: Security user {UserId} was created but Accounts profile creation FAILED ({Outcome} — {Error}).",
                normalizedProvider, normalizedEmail, newUserId, profile.Outcome,
                profile.Errors.FirstOrDefault()?.Message ?? "(no detail)");
            return AutoLinkOutcome.Refused(
                AutoLinkRefusalReason.AutoCreateFailed,
                AutoLinkPath.AutoCreate,
                candidateUserId: newUserId);
        }

        // Attach the provider link. Commits with the Auth UoW at end of Handle.
        var externalProvider = ExternalProvider.Create(
            userId:         newUserId,
            provider:       normalizedProvider,
            providerUserId: ticket.ProviderUserId,
            providerEmail:  ticket.Email);
        await externalProviderRepository.AddAsync(externalProvider, ct);

        logger.LogInformation(
            "Auto-created local identity for provider {Provider} email {Email}: new user {UserId}. Profile provisioned; link attached.",
            normalizedProvider, normalizedEmail, newUserId);

        return AutoLinkOutcome.Linked(newUserId, AutoLinkPath.AutoCreate);
    }

    /// <summary>
    /// Scenario B: local account exists — safely attach the new provider link.
    /// </summary>
    private async Task<AutoLinkOutcome> TryAutoLinkAsync(
        ExternalAuthTicket ticket,
        string normalizedProvider,
        string normalizedEmail,
        Guid candidateUserId,
        CancellationToken ct)
    {
        var userData = await securityService.GetUserDataByIdAsync(candidateUserId, ct);
        if (userData is null)
        {
            return AutoLinkOutcome.Refused(
                AutoLinkRefusalReason.LocalAccountInactive,
                AutoLinkPath.AutoLink,
                candidateUserId);
        }

        if (!userData.IsEmailVerified)
        {
            return AutoLinkOutcome.Refused(
                AutoLinkRefusalReason.LocalEmailNotVerified,
                AutoLinkPath.AutoLink,
                candidateUserId,
                localEmailVerified: false);
        }

        if (!string.Equals(
                userData.Email.Trim().ToLowerInvariant(),
                normalizedEmail,
                StringComparison.Ordinal))
        {
            return AutoLinkOutcome.Refused(
                AutoLinkRefusalReason.ResolvedEmailMismatch,
                AutoLinkPath.AutoLink,
                candidateUserId,
                localEmailVerified: true);
        }

        var existingSameProvider = await externalProviderRepository.AnyAsync(
            ep => ep.UserId == candidateUserId
               && ep.Provider == normalizedProvider
               && ep.IsActive,
            ct);

        if (existingSameProvider)
        {
            return AutoLinkOutcome.Refused(
                AutoLinkRefusalReason.UserAlreadyHasDifferentLinkOnSameProvider,
                AutoLinkPath.AutoLink,
                candidateUserId,
                localEmailVerified: true);
        }

        var takenByOther = await externalProviderRepository.AnyAsync(
            ep => ep.Provider == normalizedProvider
               && ep.ProviderUserId == ticket.ProviderUserId
               && ep.IsActive,
            ct);

        if (takenByOther)
        {
            return AutoLinkOutcome.Refused(
                AutoLinkRefusalReason.ProviderIdentityOwnedByAnotherUser,
                AutoLinkPath.AutoLink,
                candidateUserId,
                localEmailVerified: true);
        }

        var externalProvider = ExternalProvider.Create(
            userId:         candidateUserId,
            provider:       normalizedProvider,
            providerUserId: ticket.ProviderUserId,
            providerEmail:  ticket.Email);
        await externalProviderRepository.AddAsync(externalProvider, ct);

        logger.LogInformation(
            "Auto-linked provider {Provider} identity {ProviderUserId} to user {UserId} (verified email match).",
            normalizedProvider, ticket.ProviderUserId, candidateUserId);

        return AutoLinkOutcome.Linked(candidateUserId, AutoLinkPath.AutoLink);
    }

    /// <summary>Which resolver path produced the outcome — log triage.</summary>
    internal enum AutoLinkPath
    {
        Unresolved = 0,
        ExistingLink,
        AutoLink,
        AutoCreate,
    }

    /// <summary>Every possible reason external sign-in can refuse.</summary>
    internal enum AutoLinkRefusalReason
    {
        None = 0,
        TicketMissingEmail,
        ProviderDidNotVerifyEmail,
        LocalAccountInactive,
        LocalEmailNotVerified,
        ResolvedEmailMismatch,
        UserAlreadyHasDifferentLinkOnSameProvider,
        ProviderIdentityOwnedByAnotherUser,
        ConcurrentAccountCreated,
        AutoCreateFailed,
    }

    /// <summary>Unified outcome of any resolver path.</summary>
    internal readonly record struct AutoLinkOutcome(
        AutoLinkRefusalReason Reason,
        AutoLinkPath Path,
        Guid? LinkedUserId,
        Guid? CandidateUserId,
        bool LocalEmailVerified)
    {
        public static AutoLinkOutcome Linked(Guid userId, AutoLinkPath path) =>
            new(AutoLinkRefusalReason.None, path, userId, userId, LocalEmailVerified: true);

        public static AutoLinkOutcome Refused(
            AutoLinkRefusalReason reason,
            AutoLinkPath path,
            Guid? candidateUserId = null,
            bool localEmailVerified = false) =>
            new(reason, path, LinkedUserId: null, candidateUserId, localEmailVerified);
    }
}
