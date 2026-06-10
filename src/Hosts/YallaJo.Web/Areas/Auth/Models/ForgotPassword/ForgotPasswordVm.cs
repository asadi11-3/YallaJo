using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Models.ForgotPassword;

public sealed class ForgotPasswordVm
{
    // ErrorMessage values are SharedResource resx KEYS (en + ar) — see Program.cs
    // DataAnnotationLocalizerProvider (CON1).
    [Required(ErrorMessage = "Auth.Validation.EmailRequired")]
    [EmailAddress(ErrorMessage = "Auth.Validation.EmailInvalid")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Set after a successful submission to show a confirmation message.</summary>
    public string? SuccessMessage { get; set; }

    public string RecaptchaToken { get; set; } = string.Empty;
}
