namespace YallaJo.Web.Areas.Provider.Models.TourWaypoints;

// GET /api/v1/tours/{id}/waypoints
public sealed class TourWaypointResponse
{
    public Guid Id { get; init; }
    public Guid TourId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int SortOrder { get; init; }
    public bool IsMeetingPoint { get; init; }
    public int? StopDurationMinutes { get; init; }
}

// POST /api/v1/tours/{id}/waypoints
public sealed class CreateTourWaypointResponse
{
    public Guid Id { get; init; }
}
