namespace ContentTours.Application.Caching;

public static class TourGuideCacheKeys
{
    public static string List(Guid tourId)
        => $"ct:tour-guides:{tourId}";

    public static string Profile(Guid guideId)
        => $"ct:tour-guide-profile:{guideId}";

    public static string TagForTour(Guid tourId)
        => $"tour-guides:{tourId}";

    public static string TagForProfile(Guid guideId)
        => $"tour-guide-profile:{guideId}";
}
