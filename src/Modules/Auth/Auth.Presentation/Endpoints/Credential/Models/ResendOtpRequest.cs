namespace Auth.Presentation.Endpoints.Credential.Models;

public sealed record ResendOtpRequest(
    string Email,
    string Purpose,
    string RecaptchaToken);
