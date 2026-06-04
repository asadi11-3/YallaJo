using YallaJo.Web.Areas.Admin.Models.Home;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class HomeApiClient
{
    private readonly IApiClient _api;

    public HomeApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<AdminDashboardOverviewResponse>> GetOverviewAsync(CancellationToken ct = default)
        => _api.GetAsync<AdminDashboardOverviewResponse>("/api/v1/admin/dashboard", ct);

    public Task<ApiResult<AdminRevenueDashboardResponse>> GetRevenueAsync(CancellationToken ct = default)
        => _api.GetAsync<AdminRevenueDashboardResponse>("/api/v1/admin/dashboard/revenue", ct);

    public Task<ApiResult<AdminBookingsDashboardResponse>> GetBookingsAsync(CancellationToken ct = default)
        => _api.GetAsync<AdminBookingsDashboardResponse>("/api/v1/admin/dashboard/bookings", ct);

    public Task<ApiResult<AdminUsersDashboardResponse>> GetUsersAsync(CancellationToken ct = default)
        => _api.GetAsync<AdminUsersDashboardResponse>("/api/v1/admin/dashboard/users", ct);

    public Task<ApiResult<InteractionPageResponse>> GetRecentInteractionsAsync(CancellationToken ct = default)
        => _api.GetAsync<InteractionPageResponse>("/api/v1/admin/interactions?pageSize=10", ct);
}
