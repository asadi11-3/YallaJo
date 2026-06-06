using YallaJo.Web.Infrastructure.Seo;

namespace YallaJo.Web.Areas.Public.Models.Guides;

public sealed class GuideCardVm
{
    public Guid Id { get; init; }
    public string? Slug { get; init; }
    public string DisplayName { get; init; } = "";
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int TourCount { get; init; }
    public int LanguageCount { get; init; }
    public int SpecializationCount { get; init; }
    public bool HasSlug => !string.IsNullOrWhiteSpace(Slug);
    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
}

public sealed class GuidesGridVm
{
    public IReadOnlyList<GuideCardVm> Guides { get; init; } = [];
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalCount { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasResults => Guides.Count > 0;
}

public sealed class GuideTourLinkVm
{
    public Guid TourId { get; init; }
    public string Title { get; init; } = "";
    public string? Slug { get; init; }
    public bool OffersPrivateTour { get; init; }
    public bool HasSlug => !string.IsNullOrWhiteSpace(Slug);
}

public sealed class GuideDetailVm
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? AvatarUrl { get; init; }
    public string Bio { get; init; } = "";
    public int YearsOfExperience { get; init; }
    public bool HasFirstAid { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int TourCount { get; init; }
    public int LanguageCount { get; init; }
    public int SpecializationCount { get; init; }
    public IReadOnlyList<GuideTourLinkVm> Tours { get; set; } = [];
    public SeoContent? Seo { get; set; }
    public bool HasTours => Tours.Count > 0;
    public bool HasBio => !string.IsNullOrWhiteSpace(Bio);
    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
}
