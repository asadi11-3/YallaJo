using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Features.VerifyEmail.ViewModels;

public sealed class VerifyEmailVm
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email   { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required.")]
    public string OtpCode { get; set; } = string.Empty;

    public string RecaptchaToken { get; set; } = string.Empty;
}
