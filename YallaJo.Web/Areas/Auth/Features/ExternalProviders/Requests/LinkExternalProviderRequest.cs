using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders.Requests;

public sealed class LinkExternalProviderRequest
{
    [Required]
    public string Provider { get; init; } = string.Empty;
    [Required]
    public string ProviderUserId { get; init; } = string.Empty;
    public string? ProviderEmail  { get; init; }
}
