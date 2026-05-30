namespace ContentTours.Application.Caching;

public static class TourWaypointCacheKeys
{
    public static string List(Guid tourId)
        => $"ct:tour-waypoints:{tourId}";

    public static string TagForTour(Guid tourId)
        => $"tour-waypoints:{tourId}";
}
