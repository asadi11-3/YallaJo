namespace Auth.Presentation.Endpoints.ExternalProvider.Models;

/// <summary>
/// Request body for <c>POST /api/v1/auth/external-providers</c>.
/// The <see cref="Ticket"/> is produced by the Web BFF after it successfully
/// authenticates the end user against the external provider — the API never
/// accepts raw provider identifiers from untrusted clients.
/// <see cref="RecaptchaToken"/> is a reCAPTCHA v3 token guarding against bots.
/// </summary>
public sealed record LinkExternalProviderRequest(
    string Ticket,
    string RecaptchaToken);
