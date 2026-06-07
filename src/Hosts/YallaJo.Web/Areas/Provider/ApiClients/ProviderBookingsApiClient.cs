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
        string? status, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?> { ["pageSize"] = "50" };
        if (!string.IsNullOrWhiteSpace(status)) query["status"] = status;

        var url = QueryHelpers.AddQueryString($"{Base}/provider/bookings", query);
        return _api.GetAsync<ProviderBookingsPageResponse>(url, ct);
    }

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
