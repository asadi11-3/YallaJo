namespace ContentTours.Application.Caching;

public static class TourChildrenInfoCacheKeys
{
    public static string Get(Guid tourId)
        => $"ct:tour-children-info:{tourId}";

    public static string TagForTour(Guid tourId)
        => $"tour-children-info:{tourId}";
}
