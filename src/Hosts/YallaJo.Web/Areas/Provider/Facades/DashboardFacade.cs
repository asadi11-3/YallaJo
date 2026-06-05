using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Dashboard;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public sealed class DashboardFacade
{
    private readonly DashboardApiClient _api;

    public DashboardFacade(DashboardApiClient api) => _api = api;

    public async Task<ApiResult<DashboardVm>> GetDashboardAsync(CancellationToken ct = default)
    {
        var toursTask = SafeToursAsync(ct);
        var earningsTask = SafeEarningsAsync(ct);
        var joinTask = SafeJoinRequestsAsync(ct);
        var overviewTask = SafeOverviewAsync(ct);
        var pendingActionsTask = SafePendingActionsAsync(ct);
        var notificationsTask = SafeNotificationsAsync(ct);

        await Task.WhenAll(toursTask, earningsTask, joinTask, overviewTask, pendingActionsTask, notificationsTask);

        var tours = toursTask.Result;
        var earnings = earningsTask.Result;
        var joins = joinTask.Result;
        var overview = overviewTask.Result;
        var pendingActions = pendingActionsTask.Result;
        var notifications = notificationsTask.Result;

        var recentListings = tours.Items
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new RecentListingVm
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                Status = t.Status,
                BasePrice = t.BasePrice,
                Currency = t.Currency,
                CreatedAt = t.CreatedAt,
            })
            .ToList();

        var recentJoinRequests = joins
            .OrderByDescending(j => j.CreatedAt)
            .Take(5)
            .Select(j => new RecentJoinRequestVm
            {
                Id = j.Id,
                Status = j.Status,
                ParticipantCount = j.ParticipantCount,
                Message = j.Message,
                CreatedAt = j.CreatedAt,
            })
            .ToList();

        var pending = joins.Count(j => string.Equals(j.Status, "Pending", StringComparison.OrdinalIgnoreCase));

        var overviewVm = overview is null
            ? null
            : new ProviderOverviewVm
            {
                BusinessName = overview.BusinessName,
                IsApproved = overview.IsApproved,
                TotalDocuments = overview.TotalDocuments,
                ExpiredDocuments = overview.ExpiredDocuments,
                ExpiringIn30DaysDocuments = overview.ExpiringIn30DaysDocuments,
                PendingActionsCount = overview.PendingActionsCount,
                ReviewDeadline = overview.ReviewDeadline,
            };

        var pendingActionVms = pendingActions
            .Select(a => new PendingActionVm
            {
                ActionType = a.ActionType,
                Description = a.Description,
                Deadline = a.Deadline,
            })
            .ToList();

        var notificationVms = notifications
            .OrderByDescending(n => n.OccurredAt)
            .Take(5)
            .Select(n => new DashboardNotificationVm
            {
                Title = n.Title,
                Description = n.Description,
                OccurredAt = n.OccurredAt,
                IsRead = n.IsRead,
            })
            .ToList();

        var vm = new DashboardVm
        {
            TotalListings = tours.Total,
            NetEarnings = earnings?.NetEstimateTotal ?? 0m,
            EarningsCurrency = earnings?.Currency ?? string.Empty,
            PaymentCount = earnings?.PaymentCount ?? 0,
            PendingJoinRequests = pending,
            RecentListings = recentListings,
            RecentJoinRequests = recentJoinRequests,
            Overview = overviewVm,
            PendingActions = pendingActionVms,
            Notifications = notificationVms,
        };

        return ApiResult<DashboardVm>.Ok(vm);
    }

    private async Task<ProviderDashboardOverviewResponse?> SafeOverviewAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetOverviewAsync(ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<ProviderPendingActionResponse>> SafePendingActionsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetPendingActionsAsync(ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : [];
        }
        catch
        {
            return [];
        }
    }

    private async Task<List<ProviderNotificationResponse>> SafeNotificationsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetNotificationsAsync(ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : [];
        }
        catch
        {
            return [];
        }
    }

    private async Task<ListMyToursResponse> SafeToursAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetMyToursAsync(1, 5, ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : new ListMyToursResponse();
        }
        catch
        {
            return new ListMyToursResponse();
        }
    }

    private async Task<GuideEarningsSummaryResponse?> SafeEarningsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetEarningsSummaryAsync(ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<JoinRequestResponse>> SafeJoinRequestsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetJoinRequestsAsync(ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : [];
        }
        catch
        {
            return [];
        }
    }
}
