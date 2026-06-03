using YallaJo.Web.Areas.Provider.Models;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

/// <summary>
/// Typed access to the self-service provider-application endpoints
/// (<c>/api/v1/provider/*</c>). Knows endpoint URLs only; all calls go through
/// <see cref="IApiClient"/> and return <see cref="ApiResult"/>/<see cref="ApiResult{T}"/>.
/// </summary>
public sealed class ProviderApiClient
{
    private readonly IApiClient _api;

    public ProviderApiClient(IApiClient api) => _api = api;

    // GET /api/v1/provider/status
    public Task<ApiResult<ProviderStatusResponse>> GetStatusAsync(CancellationToken ct = default)
        => _api.GetAsync<ProviderStatusResponse>("/api/v1/provider/status", ct);

    // POST /api/v1/provider/register
    public Task<ApiResult<RegisterProviderResponse>> RegisterAsync(
        RegisterProviderRequest request, CancellationToken ct = default)
        => _api.PostAsync<RegisterProviderResponse>("/api/v1/provider/register", request, ct);

    // POST /api/v1/provider/apply  (submit the application for review)
    public Task<ApiResult> SubmitAsync(CancellationToken ct = default)
        => _api.PostAsync("/api/v1/provider/apply", body: null, ct);
}
