using YallaJo.Web.Areas.Auth.Models.ExternalProviders;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.ApiClients;
public sealed class ExternalProvidersApiClient
{
    private readonly IApiClient _api;
    public ExternalProvidersApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<Guid>> LinkAsync(
        LinkExternalProviderRequest request,
        CancellationToken ct = default)
        => _api.PostAsync<Guid>("/api/v1/auth/external-providers", request, ct);

    public Task<ApiResult<ExternalLoginResponse>> LoginAsync(
        ExternalLoginRequest request,
        CancellationToken ct = default)
        => _api.PostAsync<ExternalLoginResponse>("/api/v1/auth/external-providers/login", request, ct);

    public Task<ApiResult> UnlinkAsync(Guid providerId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/auth/external-providers/{providerId}", ct);
}
