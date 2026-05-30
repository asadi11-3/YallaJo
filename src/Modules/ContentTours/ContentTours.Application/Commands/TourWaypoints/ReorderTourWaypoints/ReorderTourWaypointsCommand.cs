using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourWaypoints.ReorderTourWaypoints;

public sealed record ReorderTourWaypointsCommand(
    Guid TourId,
    IReadOnlyList<Guid> WaypointIds
) : ICommand;
