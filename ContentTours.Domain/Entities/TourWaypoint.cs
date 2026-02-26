using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

public sealed class TourWaypoint : BaseEntity
{
    private TourWaypoint() { } // EF Core

    public Guid TourId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public int SortOrder { get; private set; }
    public int? DurationMinutes { get; private set; }
    public byte WaypointType { get; private set; }

    public Tour Tour { get; private set; } = default!;
}
