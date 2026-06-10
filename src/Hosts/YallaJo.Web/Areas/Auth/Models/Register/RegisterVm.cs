using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Models.Register;

// DataAnnotation ErrorMessage values are SharedResource resx KEYS (en + ar), resolved by
// the DataAnnotationLocalizerProvider configured in Program.cs (CON1). Field labels are
// localized in the SignUp view via @Localizer (no English [Display] names here).
public sealed class RegisterVm
{
    [Required(ErrorMessage = "Auth.Validation.FirstNameRequired")]
    [StringLength(100, ErrorMessage = "Auth.Validation.FirstNameLength")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Auth.Validation.LastNameRequired")]
    [StringLength(100, ErrorMessage = "Auth.Validation.LastNameLength")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Auth.Validation.EmailRequired")]
    [EmailAddress(ErrorMessage = "Auth.Validation.EmailInvalid")]
    [StringLength(320, ErrorMessage = "Auth.Validation.EmailLength")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Auth.Validation.PasswordRequired")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Auth.Validation.PasswordLength")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional post-auth destination, forwarded to the social-login challenge forms so
    /// an external sign-up started from this page lands back where the user came from
    /// (validated server-side via Url.IsLocalUrl in ExternalAuthController). The email
    /// registration path intentionally ignores it — that flow always proceeds to OTP
    /// verification first.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Populated client-side by the centralized <c>_RecaptchaField</c> partial.
    /// </summary>
    public string RecaptchaToken { get; set; } = string.Empty;
}
