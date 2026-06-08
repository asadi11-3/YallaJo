namespace ContentTours.Presentation.Endpoints.TourWaypoint.Models;

// Shape matches BFF YallaJo.Web.Areas.Provider.Models.TourWaypoints.UpdateTourWaypointApiRequest.
// Handler maps IsMeetingPoint → WaypointType (true → MeetingPoint, false → Stop),
// preserving Start/End on the existing waypoint.
public sealed record UpdateTourWaypointRequest(
    string Name,
    string? Description,
    double Latitude,
    double Longitude,
    bool IsMeetingPoint,
    int? StopDurationMinutes);
