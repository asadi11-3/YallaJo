namespace ContentPlaces.Application.Caching;

public static class ContentPlacesCacheKeys
{
    // ── Place ─────────────────────────────────────────────────────────────────

    public static string PlaceList(
        int page,
        int pageSize,
        Guid? categoryId,
        string? city,
        string? country,
        decimal? ratingMin,
        decimal? ratingMax,
        bool? hasActiveTours = null) =>
        $"cp:places:p{page}:s{pageSize}:cat:{categoryId}:city:{city}:ctry:{country}:rmin:{ratingMin}:rmax:{ratingMax}:tours:{hasActiveTours}";

    // ── ServiceItem ───────────────────────────────────────────────────────────

    public static string ServiceItemList(Guid businessId) => $"cp:biz:{businessId}:services";

    public static string ServiceItem(Guid id) => $"cp:service:{id}";

    // ── Geo ───────────────────────────────────────────────────────────────────

    public static string NearbyPlaces(double lat, double lng, double radiusKm, int pageSize)
        => $"cp:places:nearby:lat{lat:F4}:lng{lng:F4}:r{radiusKm}:s{pageSize}";

    public static string MapViewport(double n, double s, double e, double w)
        => $"cp:places:viewport:n{n:F4}:s{s:F4}:e{e:F4}:w{w:F4}";

    public static string Place(Guid id) => $"cp:place:{id}";

    public static string PlaceBySlug(string slug) => $"cp:place:slug:{slug.Trim().ToLowerInvariant()}";

    // ── Business ──────────────────────────────────────────────────────────────

    /// <summary>Paginated list of businesses for a given place. Varies by caller visibility.</summary>
    public static string BusinessListByPlace(Guid placeId, Guid? userId, bool isAdmin, int page, int pageSize) =>
        $"cp:biz:place:{placeId}:u:{userId}:a:{isAdmin}:p{page}:s{pageSize}";

    /// <summary>Single business detail. Varies by caller visibility (owner/admin see extra fields).</summary>
    public static string Business(Guid id, Guid? userId, bool isAdmin) =>
        $"cp:biz:{id}:u:{userId}:a:{isAdmin}";

    // ── BusinessHours ─────────────────────────────────────────────────────────

    /// <summary>Hours for a given business. Varies by caller visibility.</summary>
    public static string BusinessHours(Guid businessId, Guid? userId, bool isAdmin) =>
        $"cp:biz:{businessId}:hours:u:{userId}:a:{isAdmin}";
}
