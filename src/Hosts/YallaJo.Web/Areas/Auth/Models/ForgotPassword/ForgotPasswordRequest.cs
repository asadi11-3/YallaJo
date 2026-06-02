namespace YallaJo.Web.Areas.Auth.Models.ForgotPassword;

public sealed class ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
    public string RecaptchaToken { get; init; } = string.Empty;
}
