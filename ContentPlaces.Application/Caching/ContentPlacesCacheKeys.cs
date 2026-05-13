namespace ContentPlaces.Application.Caching;

public static class ContentPlacesCacheKeys
{
    // ── Broad invalidation tags ──────────────────────────────────────────────
    //
    // CONTENTPLACES-FOLLOWUP-CACHE-CENTRALIZATION-001:
    // Constants/helpers for cache tag literals. Tag VALUES are intentionally
    // unchanged from the prior raw-string usages to preserve cache-hit
    // behaviour during the refactor — this is purely a refactor that pins the
    // tag values in one canonical location and removes raw literals from
    // command/query handlers.
    //
    // Naming convention mirrors ContentToursCacheKeys (Tag* prefix for
    // invalidation tags; non-Tag methods for cache keys).

    public const string TagPlaces     = "places";
    public const string TagBusinesses = "businesses";

    /// <summary>Per-place invalidation tag — value: <c>place:{placeId}</c>.</summary>
    public static string TagForPlace(Guid placeId) => $"place:{placeId}";

    /// <summary>Per-business invalidation tag — value: <c>biz:{businessId}</c>.</summary>
    public static string TagForBusiness(Guid businessId) => $"biz:{businessId}";

    /// <summary>
    /// Per-slug invalidation tag for <see cref="PlaceBySlug"/> entries
    /// (CONTENTPLACES-FOLLOWUP-CACHE-SLUG-001).  Mirrors the ContentTours
    /// P1-005 standard so a slug rename can evict the stale slug entry
    /// without a broad <see cref="TagPlaces"/> sweep.  Slug normalization
    /// matches <see cref="PlaceBySlug"/> exactly.
    /// </summary>
    public static string TagForPlaceSlug(string slug) =>
        $"place:slug:{(slug ?? string.Empty).Trim().ToLowerInvariant()}";

    /// <summary>Per-business hours invalidation tag — value: <c>biz:{businessId}:hours</c>.</summary>
    public static string TagForBusinessHours(Guid businessId) => $"biz:{businessId}:hours";

    /// <summary>Per-business services invalidation tag — value: <c>biz:{businessId}:services</c>.</summary>
    public static string TagForBusinessServices(Guid businessId) => $"biz:{businessId}:services";

    /// <summary>Per-place businesses listing invalidation tag — value: <c>place:{placeId}:businesses</c>.</summary>
    public static string TagForPlaceBusinesses(Guid placeId) => $"place:{placeId}:businesses";

    /// <summary>Per-service-item invalidation tag — value: <c>service:{serviceItemId}</c>.</summary>
    public static string TagForServiceItem(Guid serviceItemId) => $"service:{serviceItemId}";

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

    /// <summary>
    /// Paginated list of ServiceItems for a business. Varies only by viewer tier:
    /// elevated callers (business owner OR RolePrivilegeLevel >= Admin) see all
    /// items; anonymous and standard non-owner callers see only IsAvailable=true.
    /// Two cache entries per business — one for each visibility level — eliminate
    /// the prior cache-poisoning risk where one caller's view leaked to others.
    /// </summary>
    public static string ServiceItemList(Guid businessId, bool isElevated) =>
        $"cp:biz:{businessId}:services:e:{isElevated}";

    public static string ServiceItem(Guid id) => $"cp:service:{id}";

    // ── Geo ───────────────────────────────────────────────────────────────────

    public static string NearbyPlaces(double lat, double lng, double radiusKm, int pageSize)
        => $"cp:places:nearby:lat{lat:F4}:lng{lng:F4}:r{radiusKm}:s{pageSize}";

    public static string MapViewport(double n, double s, double e, double w)
        => $"cp:places:viewport:n{n:F4}:s{s:F4}:e{e:F4}:w{w:F4}";

    public static string Place(Guid id) => $"cp:place:{id}";

    /// <summary>Backwards-compatible alias for <see cref="TagForPlace"/>.</summary>
    public static string PlaceTag(Guid placeId) => TagForPlace(placeId);

    public static string PlaceBySlug(string slug) => $"cp:place:slug:{slug.Trim().ToLowerInvariant()}";

    // ── Business ──────────────────────────────────────────────────────────────

    /// <summary>Paginated list of businesses for a given place. Varies by caller visibility.</summary>
    public static string BusinessListByPlace(Guid placeId, Guid? userId, bool isAdmin, int page, int pageSize) =>
        $"cp:biz:place:{placeId}:u:{userId}:a:{isAdmin}:p{page}:s{pageSize}";

    /// <summary>Single business detail. Varies by caller visibility (owner/admin see extra fields).</summary>
    public static string Business(Guid id, Guid? userId, bool isAdmin) =>
        $"cp:biz:{id}:u:{userId}:a:{isAdmin}";

    /// <summary>Backwards-compatible alias for <see cref="TagForBusiness"/>.</summary>
    public static string BusinessTag(Guid businessId) => TagForBusiness(businessId);

    // ── BusinessHours ─────────────────────────────────────────────────────────

    /// <summary>Hours for a given business. Varies by caller visibility.</summary>
    public static string BusinessHours(Guid businessId, Guid? userId, bool isAdmin) =>
        $"cp:biz:{businessId}:hours:u:{userId}:a:{isAdmin}";
}
