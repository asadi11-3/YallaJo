using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Models.ExternalProviders;

public sealed class ExternalLoginRequest
{
    [Required]
    public string Ticket { get; init; } = string.Empty;

    [Required]
    public string RecaptchaToken { get; init; } = string.Empty;
}
