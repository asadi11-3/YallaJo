namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;

/// <summary>
/// View model for the OAuth-callback interstitial page. The provider has
/// already authenticated the user; the page now asks the browser for a
/// reCAPTCHA v3 token (via the centralized _RecaptchaField partial) and
/// auto-POSTs the ticket + token back to the server.
/// </summary>
public sealed class ExternalAuthCompleteVm
{
    public string Provider { get; set; } = string.Empty;

    /// <summary>Signed, single-use BFF ticket — safe to embed only on this
    /// server-rendered interstitial; the page lives for a few seconds at most.</summary>
    public string Ticket { get; set; } = string.Empty;

    public string Mode { get; set; } = "login";
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// reCAPTCHA action — matches <c>login</c> / <c>link_provider</c> so the
    /// API verifier can bind the token to the right flow.
    /// </summary>
    public string RecaptchaAction { get; set; } = string.Empty;
}
