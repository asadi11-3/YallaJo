namespace YallaJo.Web.Areas.Content.Models.Guides;

public sealed class TourGuideListItemResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? DisplayName { get; init; }
    public string? Slug { get; init; }
    public string? AvatarUrl { get; init; }
    public string Bio { get; init; } = string.Empty;
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int TourCount { get; init; }
    public List<TourGuideSpecializationResponse> Specializations { get; init; } = [];
}

public sealed class TourGuideSpecializationResponse
{
    public Guid SpecializationId { get; init; }
}
