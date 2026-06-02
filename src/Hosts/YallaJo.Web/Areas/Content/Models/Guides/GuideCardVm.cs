namespace YallaJo.Web.Areas.Content.Models.Guides;

public sealed class GuideCardVm
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int TourCount { get; init; }
    public IReadOnlyList<string> SpecializationNames { get; init; } = [];

    public string GuideImageUrl { get; init; } = string.Empty;
    public string GuideImageAlt { get; init; } = string.Empty;
    public bool HasGuideImage { get; init; }

    public bool HasSlug => !string.IsNullOrWhiteSpace(Slug);
}
