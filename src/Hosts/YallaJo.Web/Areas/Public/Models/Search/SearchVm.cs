namespace YallaJo.Web.Areas.Public.Models.Search;

// Phase 4.1 note: SearchVm (the /search SSR page model) was deleted along with the
// page — /tours (TourGridVm) is the canonical search surface now. The VMs below back
// the JSON gateway endpoints (search/businesses, search/nearby, search/map).

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
