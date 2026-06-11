using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Providers;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

/// <summary>
/// Typed access to the admin provider-queue endpoints
/// (<c>/api/v1/admin/providers/*</c>). Knows endpoint URLs only; all calls go through
/// <see cref="IApiClient"/> and return <see cref="ApiResult"/>/<see cref="ApiResult{T}"/>.
/// </summary>
public sealed class ProvidersApiClient
{
    private const string Base = "/api/v1/admin/providers";

    private readonly IApiClient _api;

    public ProvidersApiClient(IApiClient api) => _api = api;

    // GET /api/v1/admin/providers?status&type&page&pageSize
    public Task<ApiResult<ProviderQueueResponse>> GetQueueAsync(
        string? status, string? type, int page, int pageSize, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        };
        if (!string.IsNullOrWhiteSpace(status)) query["status"] = status;
        if (!string.IsNullOrWhiteSpace(type)) query["type"] = type;

        var url = QueryHelpers.AddQueryString(Base, query);
        return _api.GetAsync<ProviderQueueResponse>(url, ct);
    }

    // GET /api/v1/admin/providers/status-counts
    public Task<ApiResult<ProviderQueueStatusCountsResponse>> GetStatusCountsAsync(CancellationToken ct = default)
        => _api.GetAsync<ProviderQueueStatusCountsResponse>($"{Base}/status-counts", ct);

    // GET /api/v1/admin/providers/{id}
    public Task<ApiResult<AdminProviderApplicationDetailsResponse>> GetByIdAsync(
        Guid id, CancellationToken ct = default)
        => _api.GetAsync<AdminProviderApplicationDetailsResponse>($"{Base}/{id}", ct);

    // POST /api/v1/admin/providers/{id}/approve
    public Task<ApiResult> ApproveAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/approve", body: null, ct);

    // POST /api/v1/admin/providers/{id}/reject
    public Task<ApiResult> RejectAsync(Guid id, RejectProviderRequest request, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/reject", request, ct);

    // POST /api/v1/admin/providers/{id}/request-docs
    public Task<ApiResult> RequestDocsAsync(Guid id, RequestMoreDocsRequest request, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/request-docs", request, ct);

    // POST /api/v1/admin/providers/{id}/suspend
    public Task<ApiResult> SuspendAsync(Guid id, SuspendProviderRequest request, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/suspend", request, ct);

    // POST /api/v1/admin/providers/{id}/reinstate
    public Task<ApiResult> ReinstateAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/reinstate", body: null, ct);
}
