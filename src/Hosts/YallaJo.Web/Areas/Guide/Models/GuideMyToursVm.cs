namespace YallaJo.Web.Areas.Guide.Models;

public sealed class GuideMyToursVm
{
    public bool IsGuide { get; set; }
    public IReadOnlyList<GuideTourRowVm> Tours { get; set; } = [];
    public int TotalCount { get; set; }

    public bool HasTours => Tours.Count > 0;
}

public sealed class GuideTourRowVm
{
    public Guid TourId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? OfferingStatus { get; init; }
    public bool IsProposer { get; init; }
    public bool OffersPrivateTour { get; init; }
    public DateTime? AssignedAt { get; init; }
}
