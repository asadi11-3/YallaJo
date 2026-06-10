using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Models.VerifyEmail;

public sealed class VerifyEmailVm
{
    // ErrorMessage values are SharedResource resx KEYS (en + ar) — see Program.cs
    // DataAnnotationLocalizerProvider (CON1).
    [Required(ErrorMessage = "Auth.Validation.EmailRequired")]
    [EmailAddress(ErrorMessage = "Auth.Validation.EmailInvalid")]
    public string Email   { get; set; } = string.Empty;

    [Required(ErrorMessage = "Auth.Validation.OtpRequired")]
    public string OtpCode { get; set; } = string.Empty;

    public string RecaptchaToken { get; set; } = string.Empty;
}
