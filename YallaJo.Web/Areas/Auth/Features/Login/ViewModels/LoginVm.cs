using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Features.Login.ViewModels;

public sealed class LoginVm
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
