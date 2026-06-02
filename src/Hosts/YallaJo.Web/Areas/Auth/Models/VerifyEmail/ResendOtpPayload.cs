namespace YallaJo.Web.Areas.Auth.Models.VerifyEmail;

public sealed record ResendOtpPayload(string Email, string RecaptchaToken);
