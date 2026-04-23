namespace Auth.Infrastructure.ExternalAuth;

/// <summary>
/// Wire-format constants for the internal external-auth ticket protocol.
/// Shared by the API verifier and the Web issuer through their respective
/// options bindings (both sides read/write the same claim names).
/// </summary>
public static class ExternalAuthTicketClaims
{
    /// <summary>Provider canonical name (lower-case, trimmed).</summary>
    public const string Provider = "provider";

    /// <summary>Provider-assigned unique user identifier.</summary>
    public const string ProviderUserId = "provider_user_id";

    /// <summary>Email address asserted by the provider (may be null).</summary>
    public const string Email = "email";

    /// <summary>Whether the provider has verified the email address.</summary>
    public const string EmailVerified = "email_verified";

    /// <summary>Given/first name from the provider, if present. Used by the
    /// auto-create path to seed the Security user + Accounts profile.</summary>
    public const string GivenName = "given_name";

    /// <summary>Family/last name from the provider, if present.</summary>
    public const string FamilyName = "family_name";
}
