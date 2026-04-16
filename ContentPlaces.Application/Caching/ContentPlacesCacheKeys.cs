namespace ContentPlaces.Application.Caching;

public static class ContentPlacesCacheKeys
{
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
