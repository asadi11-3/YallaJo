using YallaJo.Web.Areas.Public.Models.Directory;
using YallaJo.Web.Areas.Public.Models.Places;
using YallaJo.Web.Areas.Public.Models.Shared;

namespace YallaJo.Web.Areas.Public.Models.Home;

/// <summary>A category tile shown on the homepage.</summary>
public sealed class HomeCategoryVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Icon { get; init; }
}

/// <summary>The complete homepage view model.</summary>
public sealed class HomeVm
{
    public IReadOnlyList<TourCardVm> FeaturedTours { get; init; } = [];
    public IReadOnlyList<TourCardVm> PopularTours { get; init; } = [];
    public IReadOnlyList<PlaceCardVm> PopularPlaces { get; init; } = [];
    public IReadOnlyList<BusinessCardVm> PopularBusinesses { get; init; } = [];
    public IReadOnlyList<HomeCategoryVm> Categories { get; init; } = [];

    public bool HasFeatured => FeaturedTours.Count > 0;
    public bool HasPopular => PopularTours.Count > 0;
    public bool HasPopularPlaces => PopularPlaces.Count > 0;
    public bool HasPopularBusinesses => PopularBusinesses.Count > 0;
    public bool HasCategories => Categories.Count > 0;
}
