namespace ContentTours.Application.Caching;

public static class TourScheduleCacheKeys
{
    public static string List(Guid tourId, bool activeOnly) =>
        $"ct:tour-schedules:{tourId}:active={activeOnly}";

    public static string TagForTour(Guid tourId) => $"tour-schedules:{tourId}";
}
