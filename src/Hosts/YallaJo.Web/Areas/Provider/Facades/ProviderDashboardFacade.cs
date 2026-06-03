using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Dashboard;

namespace YallaJo.Web.Areas.Provider.Facades;

/// <summary>
/// Outcome of loading the provider dashboard, used by the controller to decide
/// whether to render or where to redirect (graceful — never a hard 403 page).
/// </summary>
public enum ProviderDashboardOutcome
{
    Ok,
    ForceSignOut,
    NotAProvider,   // 403 — caller lacks ProviderDashboard.Read (pending applicant)
    NoApplication,  // 404 — no provider application exists
    NotApproved,    // application exists but is not Approved/Suspended → send to status
    Error,          // unexpected failure
}

public sealed record ProviderDashboardResult(
    ProviderDashboardOutcome Outcome,
    ProviderDashboardVm? Dashboard = null,
    string? Error = null);

/// <summary>
/// Composes the provider dashboard over the Accounts dashboard endpoints. Reads the
/// overview first to decide accessibility; pending-actions and notifications are
/// best-effort (a failure there yields empty lists, never breaks the page).
/// </summary>
public sealed class ProviderDashboardFacade
{
    private readonly ProviderDashboardApiClient _api;

    public ProviderDashboardFacade(ProviderDashboardApiClient api) => _api = api;

    public async Task<ProviderDashboardResult> GetDashboardAsync(CancellationToken ct = default)
    {
        var overview = await _api.GetOverviewAsync(ct);

        if (overview.IsUnauthorized) return new(ProviderDashboardOutcome.ForceSignOut);
        if (overview.IsForbidden) return new(ProviderDashboardOutcome.NotAProvider);
        if (overview.IsNotFound) return new(ProviderDashboardOutcome.NoApplication);
        if (!overview.IsSuccess || overview.Data is null)
            return new(ProviderDashboardOutcome.Error,
                Error: overview.Error ?? "Could not load your provider dashboard.");

        var data = overview.Data;

        // Only an Approved (or Suspended) provider sees the dashboard; other states
        // belong on the lifecycle status page.
        var isApproved = data.IsApproved || string.Equals(data.Status, "Approved", StringComparison.OrdinalIgnoreCase);
        var isSuspended = string.Equals(data.Status, "Suspended", StringComparison.OrdinalIgnoreCase);
        if (!isApproved && !isSuspended)
            return new(ProviderDashboardOutcome.NotApproved);

        // Best-effort secondary widgets — tolerate partial failures.
        var pendingActions = await SafeListAsync(_api.GetPendingActionsAsync(ct));
        var notifications = await SafeListAsync(_api.GetNotificationsAsync(ct));

        var vm = ProviderDashboardMapper.ToVm(data, pendingActions, notifications);
        return new(ProviderDashboardOutcome.Ok, vm);
    }

    private static async Task<IReadOnlyList<T>> SafeListAsync<T>(
        Task<Infrastructure.Api.Contracts.ApiResult<List<T>>> call)
    {
        var result = await call;
        return result.IsSuccess && result.Data is not null ? result.Data : [];
    }
}
