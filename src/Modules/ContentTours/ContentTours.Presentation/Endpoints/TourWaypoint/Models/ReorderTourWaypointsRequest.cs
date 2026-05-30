namespace ContentTours.Presentation.Endpoints.TourWaypoint.Models;

public sealed record ReorderTourWaypointsRequest(IReadOnlyList<Guid> WaypointIds);
