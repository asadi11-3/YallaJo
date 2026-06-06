namespace YallaJo.Web.Areas.Public.Models.Search;

/// <summary>
/// One tour result row returned from <c>GET /api/v1/tours/search</c>.
/// Shape mirrors the public tour list payload; only fields needed for a result card are captured.
/// </summary>
public sealed class TourSearchItemResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public decimal? SalePrice { get; init; }
    public string? Currency { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public bool IsFeatured { get; init; }
}

/// <summary>
/// Per-feature paginated payload for tour search (matches the wire shape rule:
/// <c>Items / PageNumber / PageSize / TotalCount / HasPreviousPage / HasNextPage</c>; no generic wrapper, no <c>TotalPages</c>).
/// </summary>
public sealed class PaginatedTourSearchResponse
{
    public List<TourSearchItemResponse> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

/// <summary>
/// Autocomplete suggestion row returned from <c>GET /api/v1/tours/search/suggest</c>.
/// </summary>
public sealed class TourSuggestResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}

public sealed class BusinessSearchItemResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? BusinessType { get; init; }
    public string? Status { get; init; }
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

public sealed class PaginatedBusinessSearchResponse
{
    public IReadOnlyList<BusinessSearchItemResponse> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

public sealed class NearbyPlaceSearchResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public decimal AverageRating { get; init; }
    public double DistanceKm { get; init; }
}

public sealed class NearbyBusinessSearchResponse
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

public sealed class MapViewportResponse
{
    public IReadOnlyList<MapPinResponse> Pins { get; init; } = [];
    public bool IsClusteringRecommended { get; init; }
}

public sealed class MapPinResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public double Lat { get; init; }
    public double Lng { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public double AverageRating { get; init; }
    public int TourCount { get; init; }
}
