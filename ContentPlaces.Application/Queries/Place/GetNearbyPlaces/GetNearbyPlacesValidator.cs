using FluentValidation;

namespace ContentPlaces.Application.Queries.Place.GetNearbyPlaces;

public sealed class GetNearbyPlacesValidator : AbstractValidator<GetNearbyPlacesQuery>
{
    public GetNearbyPlacesValidator()
    {
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusKm).InclusiveBetween(0.1, 100);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
