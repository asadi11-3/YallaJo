using System;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourWaypoints.UpdateTourWaypoint;

// Shape matches BFF UpdateTourWaypointApiRequest (IsMeetingPoint:bool + StopDurationMinutes)
// The handler preserves existing Start/End types; only Stop↔MeetingPoint is provider-editable.
public sealed record UpdateTourWaypointCommand(
    Guid TourId,
    Guid WaypointId,
    string Name,
    string? Description,
    double Latitude,
    double Longitude,
    bool IsMeetingPoint,
    int? StopDurationMinutes
) : ICommand;
