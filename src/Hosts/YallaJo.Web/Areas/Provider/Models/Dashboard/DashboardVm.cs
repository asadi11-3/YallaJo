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

    public bool HasActivity => RecentListings.Count > 0 || RecentJoinRequests.Count > 0;
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
