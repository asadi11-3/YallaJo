using YallaJo.Web.Areas.Accounts.Models.Wishlist;

namespace YallaJo.Web.Areas.Accounts.Models.Wishlist;

/// <summary>
/// Static, allocation-only helpers for the wishlist. Per-entity hydration (which
/// fans out to several API endpoints) stays on the facade; this mapper only holds
/// pure projection / formatting helpers and the final ViewModel construction.
/// </summary>
public static class WishlistMapper
{
    public static string Humanize(string entityType) => entityType switch
    {
        "TourGuide" => "Tour guide",
        _ => entityType,
    };

    public static string JoinLocation(string? city, string? country)
    {
        var parts = new[] { city, country }.Where(s => !string.IsNullOrWhiteSpace(s));
        var joined = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(joined) ? string.Empty : joined;
    }

    /// <summary>
    /// Resolves the PUBLIC detail-page URL for a wishlist entity using the real
    /// public route templates:
    ///   Tour     => /tours/{slug}      (slug-based)
    ///   Place    => /places/{slug}     (slug-based)
    ///   Business => /businesses/{id}   (GUID-based public route)
    ///   TourGuide/Blog/unknown => no link (slug not available from the wishlist
    ///                              by-id lookups), so we return null and the view
    ///                              renders a disabled View button instead of a
    ///                              broken GUID URL.
    /// Returns <c>null</c> when a safe, valid URL cannot be built. NEVER emits a
    /// GUID for a slug-based route. Only ever uses <paramref name="entityId"/> for
    /// the Business route, which is genuinely GUID-based.
    /// </summary>
    public static string? ResolveDetailUrl(string entityType, Guid entityId, string? slug) => entityType switch
    {
        "Tour" => string.IsNullOrWhiteSpace(slug) ? null : $"/tours/{slug}",
        "Place" => string.IsNullOrWhiteSpace(slug) ? null : $"/places/{slug}",
        "Business" => $"/businesses/{entityId}",
        _ => null,
    };

    public static WishlistItemVm ToVm(
        string entityType,
        Guid entityId,
        string title,
        string? subtitle,
        string? imageUrl,
        decimal? price,
        string? currency,
        decimal? rating,
        DateTime addedAt,
        string? slug = null) => new()
    {
        EntityType = entityType,
        EntityId = entityId,
        Title = title,
        Subtitle = subtitle,
        ImageUrl = imageUrl,
        Price = price,
        Currency = currency,
        Rating = rating,
        AddedAt = addedAt,
        KindLabel = Humanize(entityType),
        DetailUrl = ResolveDetailUrl(entityType, entityId, slug),
    };
}
