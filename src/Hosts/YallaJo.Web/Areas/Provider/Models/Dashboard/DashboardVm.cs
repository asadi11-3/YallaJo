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

    public bool HasActivity => RecentListings.Count > 0 || RecentJoinRequests.Count > 0;
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
