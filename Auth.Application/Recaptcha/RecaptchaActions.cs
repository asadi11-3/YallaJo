namespace Auth.Application.Recaptcha;

/// <summary>
/// Canonical reCAPTCHA v3 action names. Each sensitive command declares the
/// action it expects to see in the token so Google-side telemetry and the
/// server-side verifier can both enforce that the token was produced by the
/// right UI flow (e.g. a token issued for "login" cannot be replayed against
/// the register endpoint).
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
