using YallaJo.Web.Areas.Public.Models.Shared;

namespace YallaJo.Web.Areas.Public.Models.Tours;

/// <summary>A category chip used for the client-side category filter.</summary>
public sealed class CategoryFilterVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}

/// <summary>
/// Server-side filters forwarded to the backend tour-search endpoint
/// (GET /api/v1/tours/search). All members are optional; when none are set the
/// grid uses the plain browse endpoint. Values are echoed back so the filter
/// offcanvas and active-filter chips stay sticky across pagination (UI-UX-S3).
/// </summary>
public sealed class TourFilterVm
{
    public decimal? PriceMin { get; init; }
    public decimal? PriceMax { get; init; }

    /// <summary>Lowercase difficulty token: easy | moderate | hard | expert.</summary>
    public string? Difficulty { get; init; }

    /// <summary>Duration window in minutes (the UI presents hour-based options).</summary>
    public int? DurationMin { get; init; }
    public int? DurationMax { get; init; }

    public bool? ChildFriendly { get; init; }
    public bool? Accessible { get; init; }
    public bool? InstantBooking { get; init; }
    public bool? HasDiscount { get; init; }

    public decimal? MinRating { get; init; }

    public bool HasAny => ActiveCount > 0;

    /// <summary>Number of active filters (drives the badge on the Filters button, UI-UX-D4).</summary>
    public int ActiveCount =>
        (PriceMin is not null ? 1 : 0) +
        (PriceMax is not null ? 1 : 0) +
        (!string.IsNullOrWhiteSpace(Difficulty) ? 1 : 0) +
        (DurationMin is not null ? 1 : 0) +
        (DurationMax is not null ? 1 : 0) +
        (ChildFriendly == true ? 1 : 0) +
        (Accessible == true ? 1 : 0) +
        (InstantBooking == true ? 1 : 0) +
        (HasDiscount == true ? 1 : 0) +
        (MinRating is not null ? 1 : 0);
}

/// <summary>Sort options surfaced on the tour grid (mapped to the API sort tokens).</summary>
public sealed class TourGridVm
{
    public IReadOnlyList<TourCardVm> Tours { get; init; } = [];
    public IReadOnlyList<CategoryFilterVm> Categories { get; init; } = [];

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }

    // Echoed filters (preserved across pagination via asp-route-*).
    public string? Query { get; init; }
    public string Sort { get; init; } = "popularity_desc";

    /// <summary>Server-side filters (Phase 4.2): forwarded to /api/v1/tours/search when any is set.</summary>
    public TourFilterVm Filters { get; init; } = new();

    // CP-3c: optional place filter (driven by /tours?placeId=...). The label is
    // hydrated from the place name when possible; falls back to a generic label.
    public Guid? PlaceId { get; init; }
    public string? PlaceFilterName { get; init; }
    public bool HasPlaceFilter => PlaceId is not null;
    public string PlaceFilterLabel => string.IsNullOrWhiteSpace(PlaceFilterName)
        ? "Tours for selected place"
        : $"Tours in {PlaceFilterName}";

    public bool HasResults => Tours.Count > 0;
}
