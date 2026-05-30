namespace YallaJo.Web.Areas.Auth.Features.ForgotPassword.Requests;

public sealed class ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
    public string RecaptchaToken { get; init; } = string.Empty;
}
