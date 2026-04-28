namespace ContentTours.Application.Caching;

public static class TourPricingTierCacheKeys
{
    public static string List(Guid tourId, bool activeOnly, string? languageCode) =>
        $"ct:tour-pricing:{tourId}:active={activeOnly}:lang:{NormalizeLanguage(languageCode)}";

    public static string TagForTour(Guid tourId) => $"tour-pricing:{tourId}";

    private static string NormalizeLanguage(string? languageCode) =>
        string.IsNullOrWhiteSpace(languageCode)
            ? "default"
            : languageCode.Trim().ToLowerInvariant();
}
