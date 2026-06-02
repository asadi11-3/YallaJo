namespace YallaJo.Web.Areas.Auth.Models.Register;

public sealed class RegisterRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string RecaptchaToken { get; init; } = string.Empty;
}
