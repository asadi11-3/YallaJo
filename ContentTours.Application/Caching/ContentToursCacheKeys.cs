namespace ContentTours.Application.Caching;


public static class ContentToursCacheKeys
{
    public const string TagToursList = "tours:list";

    public const string TagToursSearch = "tours:search";

    public const string TagToursFeatured = "tours:featured";

    public static string TagForTour(Guid tourId) => $"tour:{tourId}";

    public static string Tour(Guid id, string? acceptLanguage, bool isElevated) =>
        $"ct:tour:{id}:lang:{NormalizeLanguage(acceptLanguage)}:e:{isElevated}";

    public static string TourBySlug(string slug, string? acceptLanguage, bool isElevated) =>
        $"ct:tour:slug:{slug.Trim().ToLowerInvariant()}:lang:{NormalizeLanguage(acceptLanguage)}:e:{isElevated}";

    public static string TourList(
        int page,
        int pageSize,
        string? sort,
        string? status,
        Guid? placeId,
        bool? isFeatured,
        string? acceptLanguage,
        bool isElevated) =>
        $"ct:tours:p{page}:s{pageSize}:sort:{sort}:status:{status}:place:{placeId}:feat:{isFeatured}:lang:{NormalizeLanguage(acceptLanguage)}:e:{isElevated}";

    public static string NormalizeLanguage(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return "default";

        var first = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(first))
            return "default";

        return first
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0]
            .Trim()
            .ToLowerInvariant();
    }
}
