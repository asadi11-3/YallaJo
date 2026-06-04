namespace YallaJo.Web.Areas.Admin.Models.Home;

public sealed class AdminDashboardOverviewResponse
{
    public decimal Revenue { get; init; }
    public int Bookings { get; init; }
    public int Users { get; init; }
    public int Alerts { get; init; }
}

public sealed class AdminRevenueDashboardResponse
{
    public IReadOnlyList<RevenueTimePointResponse> Series { get; init; } = [];
    public decimal TotalRevenue { get; init; }
}

public sealed class RevenueTimePointResponse
{
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
}

public sealed class AdminBookingsDashboardResponse
{
    public int TotalBookings { get; init; }
    public int CompletedBookings { get; init; }
    public int CancelledBookings { get; init; }
}

public sealed class AdminUsersDashboardResponse
{
    public int TotalUsers { get; init; }
    public int NewUsers { get; init; }
}

public sealed class InteractionPageResponse
{
    public IReadOnlyList<InteractionResponse> Items { get; init; } = [];
    public long? NextId { get; init; }
}

public sealed class InteractionResponse
{
    public long Id { get; init; }
    public Guid? UserId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string InteractionType { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public string? UserAgent { get; init; }
}
