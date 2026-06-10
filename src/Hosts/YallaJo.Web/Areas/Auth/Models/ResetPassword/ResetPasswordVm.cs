using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Models.ResetPassword;

// DataAnnotation ErrorMessage values are SharedResource resx KEYS (en + ar), resolved by
// the DataAnnotationLocalizerProvider configured in Program.cs (CON1).
public sealed class ResetPasswordVm
{
    [Required(ErrorMessage = "Auth.Validation.EmailRequired")]
    [EmailAddress(ErrorMessage = "Auth.Validation.EmailInvalid")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Auth.Validation.OtpRequired")]
    public string OtpCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Auth.Validation.NewPasswordRequired")]
    [MinLength(8, ErrorMessage = "Auth.Validation.PasswordMinLength")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Auth.Validation.ConfirmPasswordRequired")]
    [Compare(nameof(NewPassword), ErrorMessage = "Auth.Validation.PasswordsMismatch")]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
