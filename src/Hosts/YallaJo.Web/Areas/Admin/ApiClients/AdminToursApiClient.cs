using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class AdminToursApiClient
{
    private const string Base = "/api/v1/tours";

    private readonly IApiClient _api;

    public AdminToursApiClient(IApiClient api) => _api = api;

    // GET /api/v1/tours/admin  (moderation queue across all providers)
    public Task<ApiResult<AdminToursPaginatedResponse<AdminTourSummaryResponse>>> ListAsync(
        string? status, int page, int pageSize, string? sort, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        };
        if (!string.IsNullOrWhiteSpace(status)) query["status"] = status;
        if (!string.IsNullOrWhiteSpace(sort)) query["sort"] = sort;

        var url = QueryHelpers.AddQueryString($"{Base}/admin", query);
        return _api.GetAsync<AdminToursPaginatedResponse<AdminTourSummaryResponse>>(url, ct);
    }

    // GET /api/v1/tours/{id}  (admin/owner read — returns RowVersion for privileged callers)
    public Task<ApiResult<AdminTourDetailResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<AdminTourDetailResponse>($"{Base}/{id}", ct);

    // POST /api/v1/tours/admin/{id}/approve
    public Task<ApiResult> ApproveAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/admin/{id}/approve", new TourRowVersionApiRequest(rowVersion), ct);

    // POST /api/v1/tours/admin/{id}/reject
    public Task<ApiResult> RejectAsync(Guid id, byte[] rowVersion, string reason, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/admin/{id}/reject", new RejectTourApiRequest(rowVersion, reason), ct);

    // POST /api/v1/tours/admin/{id}/suspend
    public Task<ApiResult> SuspendAsync(Guid id, byte[] rowVersion, string reason, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/admin/{id}/suspend", new SuspendTourApiRequest(rowVersion, reason), ct);

    // POST /api/v1/tours/admin/{id}/reinstate
    public Task<ApiResult> ReinstateAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/admin/{id}/reinstate", new TourRowVersionApiRequest(rowVersion), ct);

    // ── §8.4 moderation extras ───────────────────────────────────────────────────

    // PATCH /api/v1/tours/admin/{id}/feature
    public Task<ApiResult> FeatureAsync(Guid id, bool isFeatured, CancellationToken ct = default)
        => _api.PatchAsync($"{Base}/admin/{id}/feature", new ToggleTourFeaturedApiRequest(isFeatured), ct);

    // POST /api/v1/tours/proposals/{id}/approve
    public Task<ApiResult> ApproveProposalAsync(Guid id, bool isExclusive, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/proposals/{id}/approve", new ApproveTourProposalApiRequest(isExclusive), ct);

    // POST /api/v1/tours/proposals/{id}/reject
    public Task<ApiResult> RejectProposalAsync(Guid id, string reason, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/proposals/{id}/reject", new RejectReasonApiRequest(reason), ct);

    // POST /api/v1/tours/packages/{id}/approve  (no body)
    public Task<ApiResult> ApprovePackageAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/packages/{id}/approve", body: null, ct);

    // POST /api/v1/tours/packages/{id}/reject
    public Task<ApiResult> RejectPackageAsync(Guid id, string reason, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/packages/{id}/reject", new RejectReasonApiRequest(reason), ct);

    // POST /api/v1/tours/{tourId}/guide-offerings/{guideId}/suspend
    public Task<ApiResult> SuspendOfferingAsync(Guid tourId, Guid guideId, string reason, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{tourId}/guide-offerings/{guideId}/suspend", new SuspendOfferingApiRequest(reason), ct);

    // POST /api/v1/tours/{tourId}/guide-offerings/{guideId}/reinstate  (no body)
    public Task<ApiResult> ReinstateOfferingAsync(Guid tourId, Guid guideId, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{tourId}/guide-offerings/{guideId}/reinstate", body: null, ct);
}
