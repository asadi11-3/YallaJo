using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders.Requests;

/// <summary>Outbound payload for POST /api/v1/auth/external-providers.</summary>
public sealed class LinkExternalProviderRequest
{
    [Required] public string  Provider       { get; init; } = string.Empty;
    [Required] public string  ProviderUserId { get; init; } = string.Empty;
               public string? ProviderEmail  { get; init; }
}
