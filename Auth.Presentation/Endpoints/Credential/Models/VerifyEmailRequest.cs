namespace Auth.Presentation.Endpoints.Credential.Models;

public sealed record VerifyEmailRequest(
    string Email,
    string OtpCode,
    string RecaptchaToken);
