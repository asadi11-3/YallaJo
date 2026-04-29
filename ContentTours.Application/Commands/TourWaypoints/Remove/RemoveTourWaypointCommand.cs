using System;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Features.TourWaypoints.Commands.Remove;

public sealed record RemoveTourWaypointCommand(
    Guid TourId,
    Guid WaypointId
) : ICommand;
