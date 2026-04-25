namespace Auth.Presentation.Endpoints.Registration.Models;

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string RecaptchaToken);
