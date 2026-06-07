namespace YallaJo.Web.Areas.Provider.Models.TourWaypoints;

public sealed class TourWaypointsIndexVm
{
    public Guid TourId { get; init; }
    public string TourName { get; init; } = string.Empty;
    public string TourStatusLabel { get; init; } = string.Empty;

    public List<TourWaypointRowVm> Waypoints { get; init; } = [];

    public bool HasWaypoints => Waypoints.Count > 0;
}

public sealed class TourWaypointRowVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int SortOrder { get; init; }
    public bool IsMeetingPoint { get; init; }
    public int? StopDurationMinutes { get; init; }

    public string CoordinatesLabel => $"{Latitude:0.#####}, {Longitude:0.#####}";
    public string StopDurationLabel =>
        StopDurationMinutes is > 0 ? $"{StopDurationMinutes} min" : "—";
}
