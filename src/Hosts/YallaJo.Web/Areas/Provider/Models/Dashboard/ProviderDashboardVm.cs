namespace YallaJo.Web.Areas.Provider.Models.Dashboard;

/// <summary>
/// View model for the approved-provider dashboard (<c>/provider/dashboard</c>).
/// Only fields actually returned by the dashboard endpoints are surfaced — no
/// fabricated metrics.
/// </summary>
public sealed class ProviderDashboardVm
{
    // Overview.
    public string BusinessName { get; init; } = string.Empty;
    public string ProviderTypeLabel { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusBadgeClass { get; init; } = "text-bg-secondary";
    public bool IsApproved { get; init; }
    public bool IsSuspended { get; init; }

    public int TotalDocuments { get; init; }
    public int ExpiredDocuments { get; init; }
    public int ExpiringIn30DaysDocuments { get; init; }
    public int PendingActionsCount { get; init; }

    public IReadOnlyList<ProviderPendingActionVm> PendingActions { get; init; } = [];
    public IReadOnlyList<ProviderNotificationVm> Notifications { get; init; } = [];

    public bool HasPendingActions => PendingActions.Count > 0;
    public bool HasNotifications => Notifications.Count > 0;
    public bool HasDocumentWarnings => ExpiredDocuments > 0 || ExpiringIn30DaysDocuments > 0;
}

/// <summary>One actionable item the provider must address.</summary>
public sealed class ProviderPendingActionVm
{
    public string TypeLabel { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime? Deadline { get; init; }
    public string BadgeClass { get; init; } = "text-bg-secondary";
}

/// <summary>One recent provider notification (preview row).</summary>
public sealed class ProviderNotificationVm
{
    public string TypeLabel { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public bool IsRead { get; init; }
}
