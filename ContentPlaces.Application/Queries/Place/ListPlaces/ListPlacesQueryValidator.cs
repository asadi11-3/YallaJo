using FluentValidation;

namespace ContentPlaces.Application.Queries.Place.ListPlaces;

public sealed class ListPlacesQueryValidator : AbstractValidator<ListPlacesQuery>
{
    public ListPlacesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.RatingMin).InclusiveBetween(0, 5).When(x => x.RatingMin.HasValue);
        RuleFor(x => x.RatingMax).InclusiveBetween(0, 5).When(x => x.RatingMax.HasValue);
    }
}
