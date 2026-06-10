using YallaJo.Web.Areas.Public.Models.Shared;

namespace YallaJo.Web.Areas.Public.Models.Tours;

/// <summary>A category chip used for the client-side category filter.</summary>
public sealed class CategoryFilterVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
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
