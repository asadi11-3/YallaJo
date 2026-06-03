namespace YallaJo.Web.Areas.Provider.Models.Dashboard;

/// <summary>
/// Mirrors <c>ProviderDashboardOverviewResult</c> from
/// <c>GET /api/v1/provider/dashboard/overview</c>. Enum-typed API fields are modeled
/// as strings (the API serializes enums as strings).
/// </summary>
public sealed class ProviderDashboardOverviewResponse
{
    public Guid ApplicationId { get; init; }
    public string ProviderType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string BusinessName { get; init; } = string.Empty;
    public int TotalDocuments { get; init; }
    public int ExpiredDocuments { get; init; }
    public int ExpiringIn30DaysDocuments { get; init; }
    public int PendingActionsCount { get; init; }
    public DateTime? ReviewDeadline { get; init; }
    public bool IsApproved { get; init; }
}

/// <summary>Mirrors <c>ProviderPendingAction</c>.</summary>
public sealed class ProviderPendingActionResponse
{
    public string ActionType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public DateTime? Deadline { get; init; }
}

/// <summary>Mirrors <c>ProviderNotificationDto</c>.</summary>
public sealed class ProviderNotificationResponse
{
    public string NotificationType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public DateTime OccurredAt { get; init; }
    public bool IsRead { get; init; }
}
