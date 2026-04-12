using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Features.ForgotPassword.ViewModels;

public sealed class ForgotPasswordVm
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Set after a successful submission to show a confirmation message.</summary>
    public string? SuccessMessage { get; set; }
}
