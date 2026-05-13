using System;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourWaypoints.RemoveTourWaypoint;

public sealed record RemoveTourWaypointCommand(
    Guid TourId,
    Guid WaypointId
) : ICommand;
