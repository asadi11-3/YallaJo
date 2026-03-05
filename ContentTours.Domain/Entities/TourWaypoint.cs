using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Domain.Entities;

public sealed class TourWaypoint : BaseEntity
{
    private TourWaypoint() { } // EF Core

    public Guid TourId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Location Location { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public int? DurationMinutes { get; private set; }
    public byte WaypointType { get; private set; }

    public Tour Tour { get; private set; } = default!;
}
