using FluentValidation;

namespace ContentPlaces.Application.Queries.Place.GetPlaceBySlug;

public sealed class GetPlaceBySlugQueryValidator : AbstractValidator<GetPlaceBySlugQuery>
{
    public GetPlaceBySlugQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(300);
    }
}
