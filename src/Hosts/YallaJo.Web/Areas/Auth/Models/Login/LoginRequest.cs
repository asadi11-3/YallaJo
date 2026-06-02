namespace YallaJo.Web.Areas.Auth.Models.Login;

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string RecaptchaToken { get; init; } = string.Empty;
}
