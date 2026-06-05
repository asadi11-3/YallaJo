namespace YallaJo.Web.Areas.Provider.Models.Dashboard;

public sealed class ListMyToursResponse
{
    public IReadOnlyList<TourSummaryResponse> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public sealed class TourSummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal? SalePrice { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public bool IsFeatured { get; init; }
    public string? Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class GuideEarningsSummaryResponse
{
    public decimal GrossTotal { get; init; }
    public decimal NetEstimateTotal { get; init; }
    public int PaymentCount { get; init; }
    public string Currency { get; init; } = string.Empty;
}

public sealed class JoinRequestResponse
{
    public Guid Id { get; init; }
    public Guid TourBookingId { get; init; }
    public Guid UserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public string? Message { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public string? ResponseMessage { get; init; }
    public Guid? ResultingBookingId { get; init; }
    public DateTime CreatedAt { get; init; }
}

// GET /api/v1/provider/dashboard/overview
public sealed class ProviderDashboardOverviewResponse
{
    public Guid ApplicationId { get; init; }
    public int ProviderType { get; init; }
    public int Status { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public int TotalDocuments { get; init; }
    public int ExpiredDocuments { get; init; }
    public int ExpiringIn30DaysDocuments { get; init; }
    public int PendingActionsCount { get; init; }
    public DateTime? ReviewDeadline { get; init; }
    public bool IsApproved { get; init; }
}

// GET /api/v1/provider/dashboard/pending-actions
public sealed class ProviderPendingActionResponse
{
    public string ActionType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public DateTime? Deadline { get; init; }
}

// GET /api/v1/provider/dashboard/notifications
public sealed class ProviderNotificationResponse
{
    public string NotificationType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public DateTime OccurredAt { get; init; }
    public bool IsRead { get; init; }
}
