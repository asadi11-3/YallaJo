namespace YallaJo.Web.Areas.Guide.Models.Dashboard;

public sealed class DashboardVm
{
    public string DisplayName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int TourCount { get; init; }
    public int YearsOfExperience { get; init; }
    public bool HasFirstAid { get; init; }

    public decimal TotalEarned { get; init; }
    public decimal ThisMonth { get; init; }
    public decimal PendingPayout { get; init; }
    public decimal NetEarnings { get; init; }
    public string EarningsCurrency { get; init; } = string.Empty;

    public int ActiveAvailabilityBlocks { get; init; }
    public IReadOnlyList<RecentTourVm> RecentTours { get; init; } = [];

    public bool HasActivity => RecentTours.Count > 0;
}

public sealed class RecentTourVm
{
    public Guid TourId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? OfferingStatus { get; init; }
    public bool OffersPrivateTour { get; init; }
    public DateTime? AssignedAt { get; init; }
}
