using ContentTours.Application.Queries.Tour.Common;

namespace ContentTours.Application.Queries.Tour.SearchTours;

public sealed record SearchToursResult(
    IReadOnlyList<TourSummaryDto> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages,
    SearchFacets Facets,
    /// <summary>True when filtered set exceeded 5000 rows — facet counts are approximate.</summary>
    bool FacetsAreApproximate,
    SearchToursRequest AppliedFilters);
