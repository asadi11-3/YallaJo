using System;
using System.Collections.Generic;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Features.TourWaypoints.Queries.GetByTourId;

public sealed record TourWaypointResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Latitude,
    decimal Longitude,
    int SortOrder,
    int? DurationMinutes,
    string WaypointType
);

public sealed record GetTourWaypointsQuery(Guid TourId)
    : IQuery<IReadOnlyCollection<TourWaypointResponse>>;
