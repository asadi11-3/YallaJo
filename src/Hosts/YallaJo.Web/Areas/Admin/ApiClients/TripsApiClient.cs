using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Trips;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

/// <summary>
/// Typed access to the public tours endpoints (<c>/api/v1/tours</c>) used by the admin
/// Tour-approvals screen. Reads go through the privileged-visible list/detail endpoints;
/// moderation writes post to <c>/api/v1/tours/admin/{id}/*</c> with the tour's RowVersion
/// concurrency token (sent as a base64 byte array).
/// </summary>
public sealed class TripsApiClient
{
    private const string Base = "/api/v1/tours";

    private readonly IApiClient _api;

    public TripsApiClient(IApiClient api) => _api = api;

    // GET /api/v1/tours/?page&pageSize
    public Task<ApiResult<TourPageResponse>> GetToursAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString($"{Base}/", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        });
        return _api.GetAsync<TourPageResponse>(url, ct);
    }

    // GET /api/v1/tours/{id}  (privileged caller receives non-public statuses + RowVersion)
    public Task<ApiResult<TourDetailResponse>> GetTourAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<TourDetailResponse>($"{Base}/{id:D}", ct);

    // POST /api/v1/tours/admin/{id}/approve
    public Task<ApiResult> ApproveAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/admin/{id:D}/approve", new { rowVersion }, ct);

    // POST /api/v1/tours/admin/{id}/reinstate
    public Task<ApiResult> ReinstateAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/admin/{id:D}/reinstate", new { rowVersion }, ct);

    // POST /api/v1/tours/admin/{id}/reject
    public Task<ApiResult> RejectAsync(Guid id, byte[] rowVersion, string reason, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/admin/{id:D}/reject", new { rowVersion, reason }, ct);

    // POST /api/v1/tours/admin/{id}/suspend
    public Task<ApiResult> SuspendAsync(Guid id, byte[] rowVersion, string reason, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/admin/{id:D}/suspend", new { rowVersion, reason }, ct);
}
