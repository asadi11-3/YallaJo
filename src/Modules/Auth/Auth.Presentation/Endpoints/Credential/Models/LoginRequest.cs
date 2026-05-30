namespace Auth.Presentation.Endpoints.Credential.Models;

public sealed record LoginRequest(
    string Email,
    string Password,
    string RecaptchaToken);
