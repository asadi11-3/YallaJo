using ContentPlaces.Application.Queries.Place.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Place.GetNearbyPlaces;

public sealed record GetNearbyPlacesQuery(
    double Lat,
    double Lng,
    double RadiusKm = 10,
    int PageSize = 10)
    : IQuery<IReadOnlyList<NearbyPlaceSummaryDto>>;
