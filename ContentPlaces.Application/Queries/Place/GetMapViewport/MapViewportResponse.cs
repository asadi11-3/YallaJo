using ContentPlaces.Application.Queries.Place.Common;

namespace ContentPlaces.Application.Queries.Place.GetMapViewport;

public sealed record MapViewportResponse(
    IReadOnlyList<MapPinDto> Pins,
    bool IsClusteringRecommended);
