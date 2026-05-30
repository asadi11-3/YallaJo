using ContentTours.Domain.Enums;

namespace ContentTours.Presentation.Endpoints.TourWaypoint.Models;

public sealed record AddTourWaypointRequest(
    string Name,
    string? Description,
    double Latitude,
    double Longitude,
    WaypointType WaypointType,
    int? DurationMinutes);
