using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Models.AcceptInvite;

public sealed class AcceptInviteVm
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    // ErrorMessage values are SharedResource resx KEYS (en + ar) — see Program.cs
    // DataAnnotationLocalizerProvider (CON1). The label is localized in the view via
    // @Localizer["Auth.Field.ConfirmPassword"], so no English [Display] name here.
    [Required(ErrorMessage = "Auth.Validation.PasswordRequired")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Auth.Validation.PasswordLength")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Auth.Validation.ConfirmPasswordRequired")]
    [Compare(nameof(Password), ErrorMessage = "Auth.Validation.PasswordsMismatch")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
