using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders.Requests;

/// <summary>
/// Wire body for <c>POST /api/v1/auth/external-providers</c>. The API only
/// accepts the BFF-signed ticket — raw provider IDs are never sent from here.
/// </summary>
public sealed class LinkExternalProviderRequest
{
    [Required]
    public string Ticket { get; init; } = string.Empty;

    [Required]
    public string RecaptchaToken { get; init; } = string.Empty;
}
