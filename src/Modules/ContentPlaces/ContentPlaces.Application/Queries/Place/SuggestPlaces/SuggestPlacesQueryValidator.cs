using FluentValidation;

namespace ContentPlaces.Application.Queries.Place.SuggestPlaces;

public sealed class SuggestPlacesQueryValidator : AbstractValidator<SuggestPlacesQuery>
{
    public SuggestPlacesQueryValidator()
    {
        RuleFor(x => x.Q).NotEmpty().MinimumLength(2).MaximumLength(100);
    }
}
