namespace ContentTours.Application.Caching;

public static class TourCacheKeys
{
    public static string TagForTour(Guid tourId) => $"tour:{tourId}";
}
