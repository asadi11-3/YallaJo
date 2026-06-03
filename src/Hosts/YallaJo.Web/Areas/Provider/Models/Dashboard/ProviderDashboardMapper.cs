namespace YallaJo.Web.Areas.Provider.Models.Dashboard;

/// <summary>Static DTO → ViewModel mapping for the provider dashboard.</summary>
public static class ProviderDashboardMapper
{
    public static ProviderDashboardVm ToVm(
        ProviderDashboardOverviewResponse overview,
        IReadOnlyList<ProviderPendingActionResponse> pendingActions,
        IReadOnlyList<ProviderNotificationResponse> notifications) => new()
    {
        BusinessName              = overview.BusinessName,
        ProviderTypeLabel         = ProviderMapper.Humanize(overview.ProviderType),
        Status                    = overview.Status,
        StatusLabel               = ProviderMapper.Humanize(overview.Status),
        StatusBadgeClass          = BadgeClass(overview.Status),
        IsApproved                = overview.IsApproved,
        IsSuspended               = Eq(overview.Status, "Suspended"),
        TotalDocuments            = overview.TotalDocuments,
        ExpiredDocuments          = overview.ExpiredDocuments,
        ExpiringIn30DaysDocuments = overview.ExpiringIn30DaysDocuments,
        PendingActionsCount       = overview.PendingActionsCount,
        PendingActions            = pendingActions.Select(ToActionVm).ToList(),
        Notifications             = notifications.Select(ToNotificationVm).ToList(),
    };

    private static ProviderPendingActionVm ToActionVm(ProviderPendingActionResponse a) => new()
    {
        TypeLabel   = HumanizeUpperSnake(a.ActionType),
        Description = a.Description,
        Deadline    = a.Deadline,
        BadgeClass  = ActionBadgeClass(a.ActionType),
    };

    private static ProviderNotificationVm ToNotificationVm(ProviderNotificationResponse n) => new()
    {
        TypeLabel   = HumanizeUpperSnake(n.NotificationType),
        Title       = n.Title,
        Description = n.Description,
        OccurredAt  = n.OccurredAt,
        IsRead      = n.IsRead,
    };

    private static bool Eq(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static string BadgeClass(string status) => status switch
    {
        _ when Eq(status, "Approved")       => "text-bg-success",
        _ when Eq(status, "Pending")        => "text-bg-warning",
        _ when Eq(status, "MoreDocsNeeded") => "text-bg-info",
        _ when Eq(status, "Rejected")       => "text-bg-danger",
        _ when Eq(status, "Suspended")      => "text-bg-dark",
        _                                   => "text-bg-secondary",
    };

    // Severity colouring for pending-action types returned by the backend.
    private static string ActionBadgeClass(string actionType) => actionType switch
    {
        "DOCUMENT_EXPIRED"        => "text-bg-danger",
        "DOCUMENT_EXPIRING_SOON"  => "text-bg-warning",
        "MORE_DOCS_REQUIRED"      => "text-bg-info",
        "REAPPLICATION_AVAILABLE" => "text-bg-primary",
        _                         => "text-bg-secondary",
    };

    // "DOCUMENT_EXPIRING_SOON" → "Document expiring soon".
    private static string HumanizeUpperSnake(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var words = value.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select((w, i) =>
            i == 0
                ? char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()
                : w.ToLowerInvariant()));
    }
}
