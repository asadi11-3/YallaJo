using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.Places.Queries.GetMapViewport
{
    public sealed record GetMapViewportQuery(
        double NorthLat,
        double SouthLat,
        double EastLng,
        double WestLng
    ) : IRequest<MapViewportResponse>;
}
