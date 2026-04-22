namespace Auth.Infrastructure.ExternalAuth;

/// <summary>
/// Configuration for the internal external-authentication ticket protocol
/// (not for the OAuth client IDs themselves — those live in the Web layer).
///
/// <para>
/// The API and the Web BFF share <see cref="SigningKey"/>. The Web issues
/// signed tickets after successfully authenticating the user against Google,
/// Facebook, etc.; the API verifies them here.
/// </para>
/// </summary>
public sealed class ExternalAuthOptions
{
    public const string SectionName = "ExternalAuth";

    /// <summary>
    /// Shared HMAC-SHA256 signing key. Must be at least 32 bytes of entropy.
    /// Typically sourced from user secrets / environment variables in production.
    /// </summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>Expected token issuer — set to the Web BFF identity.</summary>
    public string Issuer { get; init; } = "YallaJo.Web";

    /// <summary>Expected token audience — set to the API identity.</summary>
    public string Audience { get; init; } = "YallaJo.Api";

    /// <summary>
    /// Default ticket lifetime in seconds. Web issuer SHOULD use this value;
    /// the API rejects tickets whose effective TTL exceeds this cap to prevent
    /// a compromised issuer from minting long-lived tokens.
    /// </summary>
    public int TicketLifetimeSeconds { get; init; } = 120;

    /// <summary>
    /// Providers the API will accept — names are compared case-insensitively
    /// after trimming. Requests naming a provider that isn't in this list are
    /// rejected. Keeping this server-side means adding a provider is a
    /// deliberate config change, not a client-driven surprise.
    /// </summary>
    public IReadOnlyList<string> AllowedProviders { get; init; } =
        new[] { "google", "facebook" };
}
