using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Commands.Place.UpdatePlace
{
    public sealed record UpdatePlaceResult(Guid PlaceId, string Name, string Slug);
}
