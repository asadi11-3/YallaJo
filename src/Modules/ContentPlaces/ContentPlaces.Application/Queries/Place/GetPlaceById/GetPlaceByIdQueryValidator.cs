using FluentValidation;

namespace ContentPlaces.Application.Queries.Place.GetPlaceById;

public sealed class GetPlaceByIdQueryValidator : AbstractValidator<GetPlaceByIdQuery>
{
    public GetPlaceByIdQueryValidator()
    {
        RuleFor(x => x.PlaceId).NotEmpty();
    }
}
