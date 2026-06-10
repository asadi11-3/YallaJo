using System.ComponentModel.DataAnnotations;
using YallaJo.Web.Areas.Public.Models.Directory;
using YallaJo.Web.Areas.Public.Models.Shared;
using YallaJo.Web.Infrastructure.Seo;

namespace YallaJo.Web.Areas.Public.Models.Places;

/// <summary>A single place card on the public browse grid.</summary>
public sealed class PlaceCardVm
{
    public Guid    Id            { get; init; }
    public string  Name          { get; init; } = string.Empty;
    public string  Slug          { get; init; } = string.Empty;
    public string? ImageUrl      { get; init; }
    public string  PlaceType     { get; init; } = string.Empty;
    public string? City          { get; init; }
    public string? Country       { get; init; }
    public decimal AverageRating { get; init; }
    public int     ReviewCount   { get; init; }
    public bool    IsFeatured    { get; init; }
    public bool    IsVerified    { get; init; }

    /// <summary>"City, Country" / "City" / "Country" / null when both missing.</summary>
    public string? LocationLabel => FormatLocation(City, Country);

    internal static string? FormatLocation(string? city, string? country) =>
        (city?.Trim(), country?.Trim()) switch
        {
            ({ Length: > 0 } c, { Length: > 0 } co) => $"{c}, {co}",
            ({ Length: > 0 } c, _)                   => c,
            (_, { Length: > 0 } co)                  => co,
            _                                        => null,
        };
}

/// <summary>The public places browse grid (list + pagination + echoed filters).</summary>
public sealed class PlacesGridVm
{
    public IReadOnlyList<PlaceCardVm> Places { get; init; } = [];

    public int  PageNumber      { get; init; } = 1;
    public int  PageSize        { get; init; } = 20;
    public int  TotalCount      { get; init; }
    public int  TotalPages      { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage     { get; init; }

    /// <summary>Echoed filters — keeps the form populated and pagination links filter-aware.</summary>
    public PlaceFiltersVm Filters { get; init; } = new();

    public bool HasResults => Places.Count > 0;
}

/// <summary>The public place detail page.</summary>
public sealed class PlaceDetailVm
{
    public Guid    Id                     { get; init; }
    public Guid    PlaceId                { get; init; }
    public string  Name                   { get; init; } = string.Empty;
    public string  Slug                   { get; init; } = string.Empty;
    public string? ImageUrl               { get; init; }
    public string  PlaceType              { get; init; } = string.Empty;
    public decimal Latitude               { get; init; }
    public decimal Longitude              { get; init; }
    public string? Description            { get; init; }
    public string? Address                { get; init; }
    public string? City                   { get; init; }
    public string? Country                { get; init; }
    public string? PostalCode             { get; init; }
    public string? Phone                  { get; init; }
    public string? Email                  { get; init; }
    public string? Website                { get; init; }
    public decimal AverageRating          { get; init; }
    public int     ReviewCount            { get; init; }
    public bool    IsFeatured             { get; init; }
    public bool    IsVerified             { get; init; }
    public bool    IsWheelchairAccessible { get; init; }
    public bool    HasAudioGuide          { get; init; }
    public bool    HasBrailleSignage      { get; init; }
    public string? MetaTitle              { get; init; }

    /// <summary>SEO metadata + FAQ for this place (best-effort; null when the SEO fetch failed).</summary>
    public SeoContent? Seo { get; set; }

    // CP-3c: related public (approved) tours for this place. Reuses the public
    // TourCardVm so the tour-card styling is shared. Set by the facade; tolerant
    // of a failed related-tours fetch (stays empty, never breaks the page).
    public IReadOnlyList<TourCardVm> RelatedTours { get; set; } = [];
    public bool HasRelatedTours => RelatedTours.Count > 0;

    public WeatherResponse? Weather { get; set; }

    public IReadOnlyList<PlaceAccessibilityFeatureVm> AccessibilityFeatures { get; set; } = [];
    public IReadOnlyList<PlaceAccessibilityCatalogItemVm> AccessibilityCatalog { get; set; } = [];
    public bool HasDetailedAccessibility => AccessibilityFeatures.Count > 0;

    public IReadOnlyList<BusinessCardVm> Businesses { get; set; } = [];
    public bool HasBusinesses => Businesses.Count > 0;

    public IReadOnlyList<PlaceRecommendationVm> Recommendations { get; set; } = [];
    public IReadOnlyList<PlaceRecommendationVm> SimilarRecommendations { get; set; } = [];
    public bool HasRecommendations => Recommendations.Count > 0 || SimilarRecommendations.Count > 0;

    // CP-4: real uploaded place images (absolute URLs, primary-first). Set by the
    // facade; tolerant of a failed fetch (stays empty → placeholder used).
    public IReadOnlyList<string> ImageUrls { get; set; } = [];
    public bool HasImages => ImageUrls.Count > 0;

    /// <summary>Cover image: first uploaded image, else the placeholder.</summary>
    public string HeroImageUrl => ImageUrls.Count > 0 ? ImageUrls[0] : (ImageUrl ?? string.Empty);

    /// <summary>Remaining images for the thumbnail strip (excludes the hero).</summary>
    public IReadOnlyList<string> GalleryImageUrls =>
        ImageUrls.Count > 1 ? ImageUrls.Skip(1).ToList() : [];

    public string? LocationLabel => PlaceCardVm.FormatLocation(City, Country);
    public bool HasContact => !string.IsNullOrWhiteSpace(Phone)
                              || !string.IsNullOrWhiteSpace(Email)
                              || !string.IsNullOrWhiteSpace(Website);
    public bool HasAccessibility => IsWheelchairAccessible || HasAudioGuide || HasBrailleSignage;
}

public sealed class PlaceAccessibilityFeatureVm
{
    public string FeatureType { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public bool IsAvailable { get; init; }
}

public sealed class PlaceAccessibilityCatalogItemVm
{
    public string Code { get; init; } = "";
    public string DisplayName { get; init; } = "";
}

public sealed class PlaceRecommendationVm
{
    public string Kind { get; init; } = "";
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public decimal? BasePrice { get; init; }
    public string? Currency { get; init; }
    public decimal AverageRating { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsPinned { get; init; }
    public string? BadgeText { get; init; }
    public bool IsBoosted { get; init; }

    public string RouteController => Kind switch
    {
        "Business" => "Directory",
        "Tour" => "Tours",
        "Place" => "Places",
        _ => "Home",
    };
    public string RouteAction => Kind.Equals("Business", StringComparison.OrdinalIgnoreCase) ? "Detail" : "Detail";
}

public sealed class SponsoredClickFormVm
{
    [Required]
    public Guid BidId { get; set; }

    [Required]
    public string SourceKind { get; set; } = "Place";

    [Required]
    public Guid SourceId { get; set; }

    [Range(1, 100)]
    public int Position { get; set; } = 1;

    [Range(0, int.MaxValue)]
    public int DwellTimeSeconds { get; set; }
}
