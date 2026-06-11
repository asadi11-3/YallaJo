using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Provider.Models.Bookings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

/// <summary>
/// Typed access to the Booking API for provider booking management. Endpoint URLs
/// live here only; calls go through <see cref="IApiClient"/>.
/// </summary>
public sealed class ProviderBookingsApiClient
{
    private const string Base = "/api/v1/booking";

    private readonly IApiClient _api;

    public ProviderBookingsApiClient(IApiClient api) => _api = api;

    // GET /api/v1/booking/provider/bookings — owner-scoped list
    public Task<ApiResult<ProviderBookingsPageResponse>> GetListAsync(
        string? status, string? cursor = null,
        string? fromDate = null, string? toDate = null, Guid? tourId = null,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?> { ["pageSize"] = "50" };
        if (!string.IsNullOrWhiteSpace(status)) query["status"] = status;
        if (!string.IsNullOrWhiteSpace(cursor)) query["cursor"] = cursor;
        // Phase 5: date-range + tour filters (API already supports them; YYYY-MM-DD).
        if (!string.IsNullOrWhiteSpace(fromDate)) query["fromDate"] = fromDate;
        if (!string.IsNullOrWhiteSpace(toDate)) query["toDate"] = toDate;
        if (tourId.HasValue) query["tourId"] = tourId.Value.ToString();

        var url = QueryHelpers.AddQueryString($"{Base}/provider/bookings", query);
        return _api.GetAsync<ProviderBookingsPageResponse>(url, ct);
    }

    // [Backend] B5 — GET /api/v1/booking/provider/bookings/stats — per-status counts (API7)
    public Task<ApiResult<ProviderBookingStatsResponse>> GetStatsAsync(CancellationToken ct = default)
        => _api.GetAsync<ProviderBookingStatsResponse>($"{Base}/provider/bookings/stats", ct);

    // GET /api/v1/booking/{id} — owner/provider/admin can view
    public Task<ApiResult<ProviderBookingDetailResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<ProviderBookingDetailResponse>($"{Base}/{id}", ct);

    // POST /api/v1/booking/{id}/confirm
    public Task<ApiResult> ConfirmAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/confirm", null, ct);

    // POST /api/v1/booking/{id}/cancel
    public Task<ApiResult> CancelAsync(Guid id, string reason, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/cancel", new CancelBookingApiRequest(reason), ct);

    // POST /api/v1/booking/{id}/reject
    public Task<ApiResult> RejectAsync(Guid id, string reason, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/reject", new RejectBookingApiRequest(reason), ct);

    // POST /api/v1/booking/{id}/complete — provider marks a started Confirmed booking Completed (empty body)
    public Task<ApiResult> CompleteAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/complete", null, ct);

    // GET /api/v1/tours/{id} — tour name hydration
    public Task<ApiResult<ProviderTourLookupResponse>> GetTourAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<ProviderTourLookupResponse>($"/api/v1/tours/{id}", ct);
}
