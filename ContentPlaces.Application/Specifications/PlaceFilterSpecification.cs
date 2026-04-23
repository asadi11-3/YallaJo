using ContentPlaces.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Specifications;

namespace ContentPlaces.Application.Specifications;

/// <summary>
/// Specification that encapsulates all filtering, ordering and paging logic for
/// the <c>ListPlaces</c> query.
///
/// Building the query here (Application layer) keeps <see cref="IPlaceRepository"/>
/// thin: the handler simply passes this spec to the generic
/// <c>PaginatedListAsync(spec)</c> method on the repository — no manual EF Core
/// queries in the repository or the interface.
/// </summary>
public sealed class PlaceFilterSpecification : Specification<Place>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceFilterSpecification"/> class.
    /// Initialises the specification with all optional filters plus mandatory
    /// ordering and paging.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page (caller is responsible for capping at the max).</param>
    /// <param name="categoryId">Filter to places tagged with this category ID.</param>
    /// <param name="ratingMin">Inclusive lower bound for <see cref="Place.AverageRating"/>.</param>
    /// <param name="ratingMax">Inclusive upper bound for <see cref="Place.AverageRating"/>.</param>
    /// <param name="city">Exact city match (case-sensitive, driven by DB collation).</param>
    /// <param name="country">Exact country match.</param>
    /// <param name="hasActiveTours">When true, only places with at least one active tour (<see cref="Place.TourCount"/> &gt; 0).</param>
    public PlaceFilterSpecification(
        int page,
        int pageSize,
        Guid? categoryId = null,
        decimal? ratingMin = null,
        decimal? ratingMax = null,
        string? city = null,
        string? country = null,
        bool? hasActiveTours = null)
    {
        // ── Filters ───────────────────────────────────────────────────────────
        WhereIf(categoryId.HasValue,                  p => p.CategoryId == categoryId!.Value);
        WhereIf(ratingMin.HasValue,                  p => p.AverageRating >= ratingMin!.Value);
        WhereIf(ratingMax.HasValue,                  p => p.AverageRating <= ratingMax!.Value);
        WhereIf(!string.IsNullOrWhiteSpace(city),    p => p.City    == city);
        WhereIf(!string.IsNullOrWhiteSpace(country), p => p.Country == country);
        WhereIf(hasActiveTours == true,              p => p.TourCount > 0);

        // ── Ordering: featured first → highest rating → alphabetical ─────────
        OrderByDescending(p => (object)p.IsFeatured);
        ThenByDescending(p =>  (object)p.AverageRating);
        ThenBy(p =>            (object)p.Name);

        // ── Paging ────────────────────────────────────────────────────────────
        WithPaging(page, pageSize);
    }
}
