namespace YallaJo.Web.Areas.Auth.Models.VerifyEmail;

public sealed class VerifyEmailRequest
{
    public string Email { get; init; } = string.Empty;
    public string OtpCode { get; init; } = string.Empty;
    public string RecaptchaToken { get; init; } = string.Empty;
}
