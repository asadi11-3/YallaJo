using YallaJo.Web.Areas.Provider.Models.Dashboard;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

/// <summary>
/// Typed access to the provider dashboard endpoints
/// (<c>/api/v1/provider/dashboard/*</c>). Knows endpoint URLs only; all calls go
/// through <see cref="IApiClient"/> and return <see cref="ApiResult{T}"/>.
/// </summary>
public sealed class ProviderDashboardApiClient
{
    private readonly IApiClient _api;

    public ProviderDashboardApiClient(IApiClient api) => _api = api;

    // GET /api/v1/provider/dashboard/overview
    public Task<ApiResult<ProviderDashboardOverviewResponse>> GetOverviewAsync(CancellationToken ct = default)
        => _api.GetAsync<ProviderDashboardOverviewResponse>("/api/v1/provider/dashboard/overview", ct);

    // GET /api/v1/provider/dashboard/pending-actions
    public Task<ApiResult<List<ProviderPendingActionResponse>>> GetPendingActionsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<ProviderPendingActionResponse>>("/api/v1/provider/dashboard/pending-actions", ct);

    // GET /api/v1/provider/dashboard/notifications
    public Task<ApiResult<List<ProviderNotificationResponse>>> GetNotificationsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<ProviderNotificationResponse>>("/api/v1/provider/dashboard/notifications", ct);
}
