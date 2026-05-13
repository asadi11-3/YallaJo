namespace ContentTours.Application.Caching;


public static class ContentToursCacheKeys
{
    public const string TagToursList = "tours:list";

    public const string TagToursSearch = "tours:search";

    public const string TagToursFeatured = "tours:featured";

    public const string TagToursSuggest = "tours:suggest";

    public static string TagForTour(Guid tourId) => $"tour:{tourId}";

    public static string TagForMyTours(Guid userId) => $"my-tours:{userId}";

    public static string TagForTourSlug(string slug) =>
        $"tour:slug:{(slug ?? string.Empty).Trim().ToLowerInvariant()}";

    public const string TagPackages = "packages";

    public const string TagPackagesList = "packages:list";

    public static string TagForPackage(Guid packageId) => $"package:{packageId}";

    public static string Package(Guid packageId, string? acceptLanguage) =>
        $"ct:package:{packageId}:lang:{NormalizeLanguage(acceptLanguage)}";
    public static string PackagesList(
        int page,
        int pageSize,
        Guid? providerId,
        decimal? minPrice,
        decimal? maxPrice,
        string? currency,
        Guid? includeTourId,
        DateTime effectiveDateUtc,
        string? sort) =>
        $"ct:packages:p{page}:s{pageSize}" +
        $":prov:{providerId}:min:{minPrice}:max:{maxPrice}:cur:{currency}" +
        $":incl:{includeTourId}:valid:{effectiveDateUtc:yyyyMMdd}:sort:{sort ?? "newest"}";

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

    public static string PublicTourList(
        int page,
        int pageSize,
        string? sort,
        string? status,
        Guid? placeId,
        bool? isFeatured,
        string? acceptLanguage) =>
        $"ct:tours:p{page}:s{pageSize}:sort:{sort}:status:{status}:place:{placeId}:feat:{isFeatured}:lang:{NormalizeLanguage(acceptLanguage)}:pub";

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
