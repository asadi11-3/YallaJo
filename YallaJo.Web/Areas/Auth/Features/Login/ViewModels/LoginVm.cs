using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Features.Login.ViewModels;

/// <summary>View model for the login form.</summary>
public sealed class LoginVm
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Preserved across the POST → redirect so the user lands where they came from.</summary>
    public string? ReturnUrl { get; set; }
}
