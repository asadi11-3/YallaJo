namespace Auth.Infrastructure.Recaptcha;

public sealed class RecaptchaOptions
{
    public const string SectionName = "Recaptcha";

    public string SiteKey { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public double MinimumScore { get; init; } = 0.5;

    public string VerifyEndpoint { get; init; } = "https://www.google.com/recaptcha/api/siteverify";

    public int TimeoutSeconds { get; init; } = 5;

    public bool BypassForTesting { get; init; }
}
