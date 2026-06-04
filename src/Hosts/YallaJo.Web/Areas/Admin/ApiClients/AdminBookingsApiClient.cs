using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Bookings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class AdminBookingsApiClient
{
    private const string Base = "/api/v1/booking";

    private readonly IApiClient _api;

    public AdminBookingsApiClient(IApiClient api) => _api = api;

    // GET /api/v1/booking/admin/all — cross-provider list with filters + cursor paging
    public Task<ApiResult<AdminBookingsPageResponse>> GetListAsync(
        AdminBookingFiltersVm filters, string? cursor, int pageSize, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString(),
            ["countTotal"] = "true",
        };
        if (!string.IsNullOrWhiteSpace(filters.Status)) query["status"] = filters.Status;
        if (!string.IsNullOrWhiteSpace(filters.FromDate)) query["fromDate"] = filters.FromDate;
        if (!string.IsNullOrWhiteSpace(filters.ToDate)) query["toDate"] = filters.ToDate;
        if (!string.IsNullOrWhiteSpace(filters.ProviderId)) query["providerId"] = filters.ProviderId;
        if (!string.IsNullOrWhiteSpace(filters.TourId)) query["tourId"] = filters.TourId;
        if (!string.IsNullOrWhiteSpace(filters.UserId)) query["userId"] = filters.UserId;
        if (!string.IsNullOrWhiteSpace(cursor)) query["cursor"] = cursor;

        var url = QueryHelpers.AddQueryString($"{Base}/admin/all", query);
        return _api.GetAsync<AdminBookingsPageResponse>(url, ct);
    }

    // GET /api/v1/booking/{id} — admin-readable booking details
    public Task<ApiResult<AdminBookingDetailResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<AdminBookingDetailResponse>($"{Base}/{id}", ct);

    // GET /api/v1/tours/{id} — tour name hydration
    public Task<ApiResult<AdminBookingTourLookupResponse>> GetTourAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<AdminBookingTourLookupResponse>($"/api/v1/tours/{id}", ct);
}
