namespace YallaJo.Web.Infrastructure.Security.Recaptcha;

/// <summary>
/// Web-side reCAPTCHA v3 options. Only the public <see cref="SiteKey"/> is
/// needed here — the secret lives on the API. Mirroring <see cref="MinimumScore"/>
/// is informational only; the score threshold is enforced server-side.
/// </summary>
public sealed class RecaptchaOptions
{
    public const string SectionName = "Recaptcha";

    /// <summary>Public site key for the browser.</summary>
    public string SiteKey { get; init; } = string.Empty;

    /// <summary>Informational only — the API enforces this.</summary>
    public double MinimumScore { get; init; } = 0.5;

    /// <summary>True when <see cref="SiteKey"/> is non-empty.</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(SiteKey);
}
