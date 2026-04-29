using ContentTours.Domain.Enums;
using System;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Features.TourWaypoints.Commands.Add;

public sealed record AddTourWaypointCommand(
    Guid TourId,
    string Name,
    string? Description,
    decimal Latitude,
    decimal Longitude,
    int? DurationMinutes,
    WaypointType WaypointType
) : ICommand;
