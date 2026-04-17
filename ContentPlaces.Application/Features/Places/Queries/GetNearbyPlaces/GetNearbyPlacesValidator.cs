using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.Places.Queries.GetNearbyPlaces
{
    public sealed class GetNearbyPlacesValidator : AbstractValidator<GetNearbyPlacesQuery>
    {
        public GetNearbyPlacesValidator()
        {
            RuleFor(x => x.Lat)
                .InclusiveBetween(-90, 90);

            RuleFor(x => x.Lng)
                .InclusiveBetween(-180, 180);

            RuleFor(x => x.RadiusKm)
                .InclusiveBetween(0.1, 100);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 50);
        }
    }
}
