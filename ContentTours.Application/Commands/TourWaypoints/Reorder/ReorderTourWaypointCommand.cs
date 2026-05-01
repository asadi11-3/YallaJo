using System;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Features.TourWaypoints.Commands.Reorder;

public sealed record ReorderTourWaypointCommand(
    Guid TourId,
    Guid WaypointId,
    int NewSortOrder
) : ICommand;
