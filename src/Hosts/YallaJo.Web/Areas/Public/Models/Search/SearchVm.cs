using YallaJo.Web.Areas.Public.Models.Home;

namespace YallaJo.Web.Areas.Public.Models.Search;

/// <summary>
/// Top-level view model for the §2.2 Search results page (route <c>/search</c>).
/// Holds the echoed filters + a paginated list of result cards. Result cards reuse
/// <see cref="HomeTourCardVm"/> since the card is shape-generic; future Place/Business
/// tabs can add their own collections without breaking this contract.
/// </summary>
public sealed class SearchVm
{
    public string? Query { get; init; }
    public IReadOnlyList<HomeTourCardVm> Items { get; init; } = [];
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasResults => Items.Count > 0;
    public bool HasQuery => !string.IsNullOrWhiteSpace(Query);
}

public sealed class SearchBusinessRailVm
{
    public string? Query { get; init; }
    public IReadOnlyList<SearchBusinessVm> Items { get; init; } = [];
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalCount { get; init; }
}

public sealed class SearchBusinessVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? BusinessType { get; init; }
    public double? Lat { get; init; }
    public double? Lng { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsVerified { get; init; }
    public bool IsFeatured { get; init; }
    public string? PrimaryImageUrl { get; init; }
}

public sealed class SearchNearbyVm
{
    public IReadOnlyList<SearchNearbyPlaceVm> Places { get; init; } = [];
    public IReadOnlyList<SearchNearbyBusinessVm> Businesses { get; init; } = [];
}

public sealed class SearchNearbyPlaceVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public decimal AverageRating { get; init; }
    public double DistanceKm { get; init; }
}

public sealed class SearchNearbyBusinessVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? BusinessType { get; init; }
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsVerified { get; init; }
    public bool IsFeatured { get; init; }
    public double DistanceKm { get; init; }
}

public sealed class SearchMapVm
{
    public IReadOnlyList<SearchMapPinVm> Pins { get; init; } = [];
    public bool IsClusteringRecommended { get; init; }
}

public sealed class SearchMapPinVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public double Lat { get; init; }
    public double Lng { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public double AverageRating { get; init; }
    public int TourCount { get; init; }
}
