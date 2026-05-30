namespace YallaJo.Web.Infrastructure.Security.Recaptcha;

/// <summary>
/// Mirror of <c>Auth.Application.Recaptcha.RecaptchaActions</c>. The client
/// MUST emit tokens with these exact action names or the API verifier rejects
/// them.
/// </summary>
public static class RecaptchaActions
{
    public const string Register = "register";
    public const string Login = "login";
    public const string ExternalLogin = "external_login";
    public const string LinkProvider = "link_provider";
    public const string ForgotPassword = "forgot_password";
    public const string ResetPassword = "reset_password";
    public const string VerifyEmail = "verify_email";
    public const string ResendOtp = "resend_otp";
}
