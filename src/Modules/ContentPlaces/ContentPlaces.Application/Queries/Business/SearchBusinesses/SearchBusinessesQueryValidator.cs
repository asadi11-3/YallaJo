using FluentValidation;

namespace ContentPlaces.Application.Queries.Business.SearchBusinesses;

// F40 fix 2026-05-30: /places/businesses previously silently clamped PageSize to 50
// while /places strict-rejected with 400. Standardized on strict validation (same as
// ListPlacesQueryValidator) so paging behavior is consistent across the ContentPlaces module.
public sealed class SearchBusinessesQueryValidator : AbstractValidator<SearchBusinessesQuery>
{
    public SearchBusinessesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
