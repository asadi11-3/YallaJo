using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.Places.Queries.GetMapViewport
{
    public sealed class GetMapViewportValidator : AbstractValidator<GetMapViewportQuery>
    {
        public GetMapViewportValidator()
        {
            RuleFor(x => x.NorthLat)
                .InclusiveBetween(-90, 90);
            RuleFor(x => x.SouthLat)
                .InclusiveBetween(-90, 90);
            RuleFor(x => x.EastLng)
                .InclusiveBetween(-180, 180);
            RuleFor(x => x.WestLng)
                .InclusiveBetween(-180, 180);

            RuleFor(x => x.NorthLat)
            .GreaterThan(x => x.SouthLat)
            .WithMessage("North Latitude must be greater than South Latitude");

            RuleFor(x => x.EastLng)
            .GreaterThan(x => x.WestLng)
            .WithMessage("East Longitude must be greater than West Longitude");
        }
    }
}
