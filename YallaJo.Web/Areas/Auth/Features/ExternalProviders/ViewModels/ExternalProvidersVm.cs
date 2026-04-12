using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;

public sealed class ExternalProvidersVm
{
    [Required(ErrorMessage = "Provider name is required (e.g. Google, Facebook).")]
    public string  Provider       { get; set; } = string.Empty;

    [Required(ErrorMessage = "Provider user ID is required.")]
    public string  ProviderUserId { get; set; } = string.Empty;

    [EmailAddress]
    public string? ProviderEmail  { get; set; }

    public string? Message { get; set; }
}
