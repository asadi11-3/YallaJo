namespace YallaJo.Web.Areas.Content.Models.Guides;

public sealed class GetGuideToursResponse
{
    public List<GuideTourListItemResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
}

public sealed class GuideTourListItemResponse
{
    public Guid TourId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public bool IsProposer { get; init; }
    public string OfferingStatus { get; init; } = string.Empty;
    public bool OffersPrivateTour { get; init; }
    public DateTime? AssignedAt { get; init; }
}
