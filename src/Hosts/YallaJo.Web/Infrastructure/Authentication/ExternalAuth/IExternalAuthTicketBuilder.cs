namespace YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

/// <summary>
/// Mints signed, short-lived tickets representing a successful external-provider
/// authentication performed by the Web BFF. Tickets are then POSTed to the API
/// to link the identity to the current user or to sign the user in.
/// </summary>
public interface IExternalAuthTicketBuilder
{
    /// <summary>
    /// Build a signed ticket for the given provider identity. The ticket is
    /// HMAC-SHA256 signed using the shared secret and carries a unique nonce
    /// (<c>jti</c>) that the API consumes exactly once.
    /// </summary>
    /// <param name="firstName">Given name surfaced by the provider. Used by
    /// the API's auto-create path to seed User.FirstName / Profile.FirstName
    /// on first sign-in when no local account exists. Null/blank is tolerated —
    /// the API will fall back to a safe default.</param>
    /// <param name="lastName">Family name surfaced by the provider. Same
    /// rules as <paramref name="firstName"/>.</param>
    string Build(
        string provider,
        string providerUserId,
        string? email,
        bool emailVerifiedByProvider,
        string? firstName = null,
        string? lastName = null);
}
