using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Place.GetMapViewport;

public sealed record GetMapViewportQuery(
    double NorthLat,
    double SouthLat,
    double EastLng,
    double WestLng)
    : IQuery<MapViewportResponse>;
