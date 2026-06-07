namespace YallaJo.Web.Areas.Provider.Models.TourWaypoints;

// POST /api/v1/tours/{id}/waypoints
public sealed record CreateTourWaypointApiRequest(
    string Name,
    string? Description,
    double Latitude,
    double Longitude,
    bool IsMeetingPoint,
    int? StopDurationMinutes);

// PUT /api/v1/tours/{id}/waypoints/{waypointId}
public sealed record UpdateTourWaypointApiRequest(
    string Name,
    string? Description,
    double Latitude,
    double Longitude,
    bool IsMeetingPoint,
    int? StopDurationMinutes);

// PUT /api/v1/tours/{id}/waypoints/reorder — ordered list of waypoint ids.
public sealed record ReorderTourWaypointsApiRequest(IReadOnlyList<Guid> WaypointIds);
