using ContentTours.Domain.Enums;
using System;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourWaypoints.AddTourWaypoint;

public sealed record AddTourWaypointCommand(
    Guid TourId,
    string Name,
    string? Description,
    double Latitude,
    double Longitude,
    WaypointType WaypointType,
    int? DurationMinutes
) : ICommand<AddTourWaypointResult>;
