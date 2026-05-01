using ContentTours.Domain.Enums;
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
    public WaypointType WaypointType { get; private set; }

    public Tour Tour { get; private set; } = default!;


    public static TourWaypoint Create(Guid tourId, string name, string? description, Location location, int? durationMinutes, int sorteOrder, WaypointType waypointType)
    {
        return new TourWaypoint
        {
            TourId = tourId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Location = location,
            SortOrder = sorteOrder,
            DurationMinutes = durationMinutes,
            WaypointType = waypointType
        };
    }

    public void Update(string name, string? description, Location location, int? durationMinutes, WaypointType waypointType)
    {
        Name = name.Trim();
        Description = description?.Trim();
        Location = location;
        DurationMinutes = durationMinutes;
        WaypointType = waypointType;
    }

    public void SetSortOrder(int sortOrder)
    {
       if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order cannot be negative.");
        }

       SortOrder = sortOrder;
    }
}
