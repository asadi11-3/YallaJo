namespace Auth.Application.ExternalAuth;

/// <summary>
/// Verified, trusted claims extracted from a short-lived signed ticket that the
/// Web BFF issues after it successfully authenticates the end user against an
/// external OAuth/OIDC provider (Google, Facebook, ...).
///
/// <para>
/// Tickets are the ONLY way the API accepts external-provider identities:
/// raw <c>ProviderUserId</c> values are never trusted when they come directly
/// from an untrusted HTTP client, because a hostile caller could otherwise
/// claim to be any Google account.
/// </para>
///
/// <para>
/// The ticket is signed with a shared HMAC secret (<see cref="Infrastructure.ExternalAuth.ExternalAuthOptions"/>)
/// and carries a unique <see cref="TicketId"/> — consumed exactly once via
/// <see cref="IExternalAuthNonceStore"/> to block replay attacks.
/// </para>
/// </summary>
public sealed record ExternalAuthTicket(
    Guid TicketId,
    string Provider,
    string ProviderUserId,
    string? Email,
    bool EmailVerifiedByProvider,
    DateTime IssuedAt,
    DateTime ExpiresAt);
