using FluentValidation;

namespace ContentPlaces.Application.Queries.Place.GetMapViewport;

public sealed class GetMapViewportValidator : AbstractValidator<GetMapViewportQuery>
{
    public GetMapViewportValidator()
    {
        RuleFor(x => x.NorthLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.SouthLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.EastLng).InclusiveBetween(-180, 180);
        RuleFor(x => x.WestLng).InclusiveBetween(-180, 180);

        RuleFor(x => x.NorthLat)
            .GreaterThan(x => x.SouthLat)
            .WithMessage("North latitude must be greater than south latitude");

        RuleFor(x => x.EastLng)
            .GreaterThan(x => x.WestLng)
            .WithMessage("East longitude must be greater than west longitude");
    }
}
