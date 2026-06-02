using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Models.Login;

public sealed class LoginVm
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Populated client-side by the centralized <c>_RecaptchaField</c> partial
    /// before the form is submitted. Never displayed in the UI.
    /// </summary>
    public string RecaptchaToken { get; set; } = string.Empty;
}
