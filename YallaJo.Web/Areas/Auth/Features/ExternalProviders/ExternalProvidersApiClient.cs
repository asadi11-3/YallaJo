using YallaJo.Web.Areas.Auth.Features.ExternalProviders.Requests;
using YallaJo.Web.Areas.Auth.Features.ExternalProviders.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders;

public sealed class ExternalProvidersApiClient
{
    private readonly ApiClient _api;
    public ExternalProvidersApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<LinkExternalProviderResponse>> LinkAsync(
        LinkExternalProviderRequest request, CancellationToken ct = default)
        => _api.PostAsync<LinkExternalProviderResponse>("/api/v1/auth/external-providers", request, ct);

    public Task<ApiResult<ExternalLoginResponse>> LoginAsync(
        ExternalLoginRequest request, CancellationToken ct = default)
        => _api.PostAsync<ExternalLoginResponse>("/api/v1/auth/external-providers/login", request, ct);

    public Task<ApiResult> UnlinkAsync(Guid providerId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/auth/external-providers/{providerId}", ct);
}
