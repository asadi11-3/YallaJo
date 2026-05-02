using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Tests.Unit.Ezz;

internal static class TestWaypointFactory
{
    /// <summary>
    /// Creates a <see cref="TourWaypoint"/> via the domain factory.
    /// Defaults to <see cref="WaypointType.Stop"/> so tests that only need a
    /// generic waypoint don't conflict with Single-Start / Single-End invariants.
    /// </summary>
    public static TourWaypoint Create(
        Guid tourId,
        string name = "Petra Gate",
        WaypointType waypointType = WaypointType.Stop,
        int sortOrder = 0,
        decimal latitude = 30.32m,
        decimal longitude = 35.45m)
    {
        return TourWaypoint.Create(
            tourId:          tourId,
            name:            name,
            description:     null,
            location:        new Location(latitude, longitude),
            waypointType:    waypointType,
            sortOrder:       sortOrder,
            durationMinutes: null);
    }
}
