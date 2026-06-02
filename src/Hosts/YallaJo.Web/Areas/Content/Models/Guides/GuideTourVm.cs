namespace YallaJo.Web.Areas.Content.Models.Guides;

public sealed class GuideTourVm
{
    public Guid TourId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string OfferingStatus { get; init; } = string.Empty;
    public bool OffersPrivateTour { get; init; }
}
