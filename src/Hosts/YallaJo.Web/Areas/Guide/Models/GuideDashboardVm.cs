namespace YallaJo.Web.Areas.Guide.Models;

public sealed class GuideDashboardVm
{
    public bool IsGuide { get; set; }
    public string? DisplayName { get; set; }
    public int TourCount { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int YearsOfExperience { get; set; }

    public decimal NetEarnings { get; set; }
    public decimal ThisMonth { get; set; }
    public decimal PendingPayout { get; set; }
    public string EarningsCurrency { get; set; } = string.Empty;

    public IReadOnlyList<GuideTourCardVm> RecentTours { get; set; } = [];
}

public sealed class GuideTourCardVm
{
    public Guid TourId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? OfferingStatus { get; init; }
    public bool IsProposer { get; init; }
}
