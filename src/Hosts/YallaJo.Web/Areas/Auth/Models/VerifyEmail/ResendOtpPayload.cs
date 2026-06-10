namespace YallaJo.Web.Areas.Auth.Models.VerifyEmail;

/// <summary>
/// Body of the AJAX resend-OTP call. <c>Email</c> is legacy and IGNORED — the server
/// resolves the target e-mail from the encrypted pending-verification cookie, so the
/// client cannot direct OTPs at an arbitrary address.
/// </summary>
public sealed record ResendOtpPayload(string? Email, string RecaptchaToken);
