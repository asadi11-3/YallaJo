namespace ContentTours.Application.Caching;

public static class TourGuideCacheKeys
{
    public static string List(Guid tourId)
        => $"ct:tour-guides:{tourId}";

    public static string TagForTour(Guid tourId)
        => $"tour-guides:{tourId}";
}
