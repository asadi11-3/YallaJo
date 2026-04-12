namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders.Responses;

/// <summary>POST /api/v1/auth/external-providers returns the new provider record's Id.</summary>
public sealed class LinkExternalProviderResponse
{
    public Guid Id { get; init; }
}
