namespace Auth.Infrastructure.Recaptcha;

/// <summary>
/// Configuration for Google reCAPTCHA v3 server-side verification.
/// <para>
/// <see cref="SiteKey"/> is NOT used on the API — it's mirrored here so the
/// operator can see the full config at a glance; the value that actually
/// matters on the server is <see cref="SecretKey"/>.
/// </para>
/// <para>
/// <see cref="MinimumScore"/> is the lower bound for accepting a request.
/// Google returns a score in [0.0, 1.0] where 1.0 is "very likely a good
/// interaction" and 0.0 is "very likely a bot".
/// </para>
/// </summary>
public sealed class RecaptchaOptions
{
    public const string SectionName = "Recaptcha";

    /// <summary>Public site key embedded in the Web client.</summary>
    public string SiteKey { get; init; } = string.Empty;

    /// <summary>
    /// Server-only secret key used to call Google's <c>siteverify</c> endpoint.
    /// MUST be supplied via user-secrets / environment variables in production.
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// Minimum score required to accept a request. Google recommends a default
    /// of 0.5 and raising or lowering it based on observed traffic patterns.
    /// </summary>
    public double MinimumScore { get; init; } = 0.5;

    /// <summary>
    /// Google's siteverify endpoint. Kept configurable for test fixtures and
    /// for regions that may route through alternative endpoints.
    /// </summary>
    public string VerifyEndpoint { get; init; } = "https://www.google.com/recaptcha/api/siteverify";

    /// <summary>
    /// HTTP timeout for a single verification call. Kept tight because the
    /// check runs inline on every sensitive request.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 5;

    /// <summary>
    /// When true, the verifier bypasses Google and accepts any token. Intended
    /// ONLY for integration tests / local dev with a reCAPTCHA-less client.
    /// Set to <c>false</c> (the default) in production.
    /// </summary>
    public bool BypassForTesting { get; init; }
}
