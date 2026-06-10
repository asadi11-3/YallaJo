using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Models.Login;

// DataAnnotation ErrorMessage values are SharedResource resx KEYS (en + ar), resolved by
// the DataAnnotationLocalizerProvider configured in Program.cs (CON1).
public sealed class LoginVm
{
    [Required(ErrorMessage = "Auth.Validation.EmailRequired")]
    [EmailAddress(ErrorMessage = "Auth.Validation.EmailInvalid")]
    public string Email { get; set; } = string.Empty;

    // DataType.Password makes the tag helper emit type="password" (the SignIn view also
    // sets it explicitly) so the secret is never rendered as a plain text input.
    [Required(ErrorMessage = "Auth.Validation.PasswordRequired")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Populated client-side by the centralized <c>_RecaptchaField</c> partial
    /// before the form is submitted. Never displayed in the UI.
    /// </summary>
    public string RecaptchaToken { get; set; } = string.Empty;
}
