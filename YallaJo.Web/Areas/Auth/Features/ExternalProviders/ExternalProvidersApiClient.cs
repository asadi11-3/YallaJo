using YallaJo.Web.Areas.Auth.Features.ExternalProviders.Requests;
using YallaJo.Web.Areas.Auth.Features.ExternalProviders.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders;

public sealed class ExternalProvidersApiClient
{
    private readonly ApiClient _api;
    public ExternalProvidersApiClient(ApiClient api) => _api = api;

    /// <summary>POST /api/v1/auth/external-providers — link OAuth provider.</summary>
    public Task<ApiResult<LinkExternalProviderResponse>> LinkAsync(
        LinkExternalProviderRequest request, CancellationToken ct = default)
        => _api.PostAsync<LinkExternalProviderResponse>("/api/v1/auth/external-providers", request, ct);

    /// <summary>DELETE /api/v1/auth/external-providers/{providerId} — unlink.</summary>
    public Task<ApiResult> UnlinkAsync(Guid providerId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/auth/external-providers/{providerId}", ct);
}
