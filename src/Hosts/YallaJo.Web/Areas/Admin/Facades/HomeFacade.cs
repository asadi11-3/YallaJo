using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Home;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class HomeFacade
{
    private readonly HomeApiClient _api;

    public HomeFacade(HomeApiClient api) => _api = api;

    public async Task<ApiResult<DashboardVm>> GetDashboardAsync(CancellationToken ct = default)
    {
        var overview = await _api.GetOverviewAsync(ct);
        if (overview.IsUnauthorized)
        {
            return ApiResult<DashboardVm>.ForceSignOut();
        }

        if (overview is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<DashboardVm>.Fail(overview.StatusCode, overview.Error ?? "Could not load the dashboard.");
        }

        var revenueTask = _api.GetRevenueAsync(ct);
        var bookingsTask = _api.GetBookingsAsync(ct);
        var usersTask = _api.GetUsersAsync(ct);
        var interactionsTask = _api.GetRecentInteractionsAsync(ct);

        await Task.WhenAll(revenueTask, bookingsTask, usersTask, interactionsTask);

        var revenue = revenueTask.Result is { IsSuccess: true, Data: not null } r ? r.Data : null;
        var bookings = bookingsTask.Result is { IsSuccess: true, Data: not null } b ? b.Data : null;
        var users = usersTask.Result is { IsSuccess: true, Data: not null } u ? u.Data : null;
        var interactions = interactionsTask.Result is { IsSuccess: true, Data: not null } i ? i.Data : null;

        var vm = new DashboardVm
        {
            Revenue = overview.Data.Revenue,
            Bookings = overview.Data.Bookings,
            Users = overview.Data.Users,
            Alerts = overview.Data.Alerts,
            TotalRevenue = revenue?.TotalRevenue ?? 0m,
            RevenueSeries = revenue?.Series
                .Select(s => new RevenuePointVm { Date = s.Date, Revenue = s.Revenue })
                .ToList() ?? [],
            TotalBookings = bookings?.TotalBookings ?? 0,
            CompletedBookings = bookings?.CompletedBookings ?? 0,
            CancelledBookings = bookings?.CancelledBookings ?? 0,
            TotalUsers = users?.TotalUsers ?? 0,
            NewUsers = users?.NewUsers ?? 0,
            RecentInteractions = interactions?.Items
                .Select(x => new InteractionRowVm
                {
                    InteractionType = x.InteractionType,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    OccurredAt = x.OccurredAt
                })
                .ToList() ?? []
        };

        return ApiResult<DashboardVm>.Ok(vm);
    }
}
