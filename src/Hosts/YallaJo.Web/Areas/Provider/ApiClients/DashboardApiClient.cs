using YallaJo.Web.Areas.Provider.Models.Dashboard;
// Aliased to avoid clashing with the Dashboard-namespace mirror types of the same names.
using ProviderBookingStatsResponse = YallaJo.Web.Areas.Provider.Models.Bookings.ProviderBookingStatsResponse;
using ProviderEarningsSummaryResponse = YallaJo.Web.Areas.Provider.Models.Earnings.ProviderEarningsSummaryResponse;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class DashboardApiClient
{
    private readonly IApiClient _api;

    public DashboardApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<ListMyToursResponse>> GetMyToursAsync(int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<ListMyToursResponse>($"/api/v1/tours/provider/my-tours?page={page}&pageSize={pageSize}", ct);

    public Task<ApiResult<GuideEarningsSummaryResponse>> GetEarningsSummaryAsync(CancellationToken ct = default)
        => _api.GetAsync<GuideEarningsSummaryResponse>("/api/v1/finance/guide/summary", ct);

    // [Backend] B4 — richer earnings summary (this-month / pending payout / commission).
    public Task<ApiResult<ProviderEarningsSummaryResponse>> GetProviderEarningsSummaryAsync(CancellationToken ct = default)
        => _api.GetAsync<ProviderEarningsSummaryResponse>("/api/v1/finance/provider/summary", ct);

    // [Backend] B5 — per-status booking counts for the KPI row / donut chart.
    public Task<ApiResult<ProviderBookingStatsResponse>> GetBookingStatsAsync(CancellationToken ct = default)
        => _api.GetAsync<ProviderBookingStatsResponse>("/api/v1/booking/provider/bookings/stats", ct);

    public Task<ApiResult<List<JoinRequestResponse>>> GetJoinRequestsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<JoinRequestResponse>>("/api/v1/booking/join-requests?myRequestsOnly=false", ct);

    public Task<ApiResult<ProviderDashboardOverviewResponse>> GetOverviewAsync(CancellationToken ct = default)
        => _api.GetAsync<ProviderDashboardOverviewResponse>("/api/v1/provider/dashboard/overview", ct);

    public Task<ApiResult<List<ProviderPendingActionResponse>>> GetPendingActionsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<ProviderPendingActionResponse>>("/api/v1/provider/dashboard/pending-actions", ct);

    public Task<ApiResult<List<ProviderNotificationResponse>>> GetNotificationsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<ProviderNotificationResponse>>("/api/v1/provider/dashboard/notifications", ct);
}
