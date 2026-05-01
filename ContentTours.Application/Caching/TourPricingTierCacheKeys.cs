namespace ContentTours.Application.Caching;

/// <summary>
/// Cache-key helpers for TourPricingTier queries.
///
/// IMPORTANT — visibility partitioning (P1 #2 / ERR-cross-tier-cache-poisoning):
/// the public ListTourPricingTiers endpoint forces <c>activeOnly = true</c> for
/// non-elevated callers but allows owner/admin to see inactive tiers when
/// <c>activeOnly = false</c>. If the cache key did not encode the caller's
/// visibility, an admin's "all tiers" response could be served to an anonymous
/// caller (or vice versa). Every visibility-sensitive read therefore stamps a
/// <c>:viz=...</c> token computed from <c>CallerUserId</c> + <c>IsAdmin</c>:
///
///   - Anonymous caller          → <c>viz=anon</c>
///   - Admin (any identity)      → <c>viz=adm</c>     (admins all see the same data)
///   - Authenticated user X      → <c>viz=usr-{X}</c> (owner of THIS tour and a
///                                                     non-owner authenticated user
///                                                     each get their own bucket)
///
/// Owner-of-this-tour vs. non-owner-authenticated-user response content is
/// currently identical (both forced to active-only when the caller is not admin
/// AND not the owner). The per-user bucket is intentional — it's a small cache-
/// fill cost for a hard correctness guarantee against cross-tier leakage.
/// </summary>
public static class TourPricingTierCacheKeys
{
    public static string List(
        Guid tourId,
        bool activeOnly,
        string? languageCode,
        Guid? callerUserId,
        bool isAdmin)
    {
        var viz = ResolveVisibilityToken(callerUserId, isAdmin);
        return $"ct:tour-pricing:{tourId}:active={activeOnly}:viz={viz}:lang:{NormalizeLanguage(languageCode)}";
    }

    public static string TagForTour(Guid tourId) => $"tour-pricing:{tourId}";

    private static string ResolveVisibilityToken(Guid? callerUserId, bool isAdmin)
    {
        if (isAdmin) return "adm";
        return callerUserId.HasValue ? $"usr-{callerUserId.Value}" : "anon";
    }

    private static string NormalizeLanguage(string? languageCode) =>
        string.IsNullOrWhiteSpace(languageCode)
            ? "default"
            : languageCode.Trim().ToLowerInvariant();
}
