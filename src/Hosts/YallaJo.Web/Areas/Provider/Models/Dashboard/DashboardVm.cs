namespace YallaJo.Web.Areas.Provider.Models.Dashboard;

public sealed class DashboardVm
{
    public int TotalListings { get; init; }
    public decimal NetEarnings { get; init; }
    public string EarningsCurrency { get; init; } = string.Empty;
    public int PaymentCount { get; init; }
    public int PendingJoinRequests { get; init; }
    public IReadOnlyList<RecentListingVm> RecentListings { get; init; } = [];
    public IReadOnlyList<RecentJoinRequestVm> RecentJoinRequests { get; init; } = [];

    // From GET /api/v1/provider/dashboard/overview (null when unavailable)
    public ProviderOverviewVm? Overview { get; init; }

    // From GET /api/v1/provider/dashboard/pending-actions and /notifications
    public IReadOnlyList<PendingActionVm> PendingActions { get; init; } = [];
    public IReadOnlyList<DashboardNotificationVm> Notifications { get; init; } = [];

    // [Backend] B4 — from GET /api/v1/finance/provider/summary (null when unavailable, ERR3 soft-degrade)
    public ProviderEarningsKpisVm? EarningsKpis { get; init; }

    // [Backend] B5 — from GET /api/v1/booking/provider/bookings/stats (null when unavailable)
    public BookingStatsVm? BookingStats { get; init; }

    public bool HasActivity => RecentListings.Count > 0 || RecentJoinRequests.Count > 0;
}

/// <summary>[Backend] B4 — richer earnings KPIs for the dashboard.</summary>
public sealed class ProviderEarningsKpisVm
{
    public decimal GrossTotal { get; init; }
    public decimal NetEarnings { get; init; }
    public decimal ThisMonth { get; init; }
    public decimal PendingPayout { get; init; }
    public decimal TotalCommission { get; init; }
    public string Currency { get; init; } = string.Empty;
}

/// <summary>[Backend] B5 — booking per-status counts (drives the dashboard donut chart, X14).</summary>
public sealed class BookingStatsVm
{
    public int Total { get; init; }
    public int Pending { get; init; }
    public int Confirmed { get; init; }
    public int Completed { get; init; }
    public int Cancelled { get; init; }
    public int Rejected { get; init; }
}

public sealed class ProviderOverviewVm
{
    public string BusinessName { get; init; } = string.Empty;
    public bool IsApproved { get; init; }
    public int TotalDocuments { get; init; }
    public int ExpiredDocuments { get; init; }
    public int ExpiringIn30DaysDocuments { get; init; }
    public int PendingActionsCount { get; init; }
    public DateTime? ReviewDeadline { get; init; }

    public bool HasDocumentAlerts => ExpiredDocuments > 0 || ExpiringIn30DaysDocuments > 0;
}

public sealed class PendingActionVm
{
    public string ActionType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime? Deadline { get; init; }
}

public sealed class DashboardNotificationVm
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public bool IsRead { get; init; }
}

public sealed class RecentListingVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Status { get; init; }
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class RecentJoinRequestVm
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public string? Message { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsPending => string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase);
}
