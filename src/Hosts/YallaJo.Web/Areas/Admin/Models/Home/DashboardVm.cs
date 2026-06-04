namespace YallaJo.Web.Areas.Admin.Models.Home;

public sealed class DashboardVm
{
    public decimal Revenue { get; init; }
    public int Bookings { get; init; }
    public int Users { get; init; }
    public int Alerts { get; init; }

    public decimal TotalRevenue { get; init; }
    public IReadOnlyList<RevenuePointVm> RevenueSeries { get; init; } = [];

    public int TotalBookings { get; init; }
    public int CompletedBookings { get; init; }
    public int CancelledBookings { get; init; }

    public int TotalUsers { get; init; }
    public int NewUsers { get; init; }

    public IReadOnlyList<InteractionRowVm> RecentInteractions { get; init; } = [];

    public bool HasRevenueSeries => RevenueSeries.Count > 0;
    public bool HasInteractions => RecentInteractions.Count > 0;

    // Other-than-completed/cancelled bookings (in-progress / pending), for the donut.
    public int OtherBookings => Math.Max(0, TotalBookings - CompletedBookings - CancelledBookings);
}

public sealed class RevenuePointVm
{
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
}

public sealed class InteractionRowVm
{
    public string InteractionType { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public DateTime OccurredAt { get; init; }
}
