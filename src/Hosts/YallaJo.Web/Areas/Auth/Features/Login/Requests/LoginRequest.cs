namespace YallaJo.Web.Areas.Auth.Features.Login.Requests;

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string RecaptchaToken { get; init; } = string.Empty;
}
