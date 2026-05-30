namespace Auth.Presentation.Endpoints.ExternalProvider.Models;

/// <summary>
/// Request body for <c>POST /api/v1/auth/external-providers/login</c>.
/// </summary>
public sealed record ExternalLoginRequest(
    string Ticket,
    string RecaptchaToken);
