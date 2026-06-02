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

    // Public detail-page URL per entity kind. NOTE: these are the exact URLs that
    // previously lived inline in the wishlist view; preserved verbatim here.
    // (Route-validity of some of these — e.g. /tours/{id} vs /tours/{slug}, and the
    // existence of /places and /blogs routes — is a separate, deferred concern.)
    public static string ResolveDetailUrl(string entityType, Guid entityId) => entityType switch
    {
        "Tour" => $"/tours/{entityId}",
        "Place" => $"/places/{entityId}",
        "Business" => $"/places/businesses/{entityId}",
        "TourGuide" => $"/guides/{entityId}",
        "Blog" => $"/blogs/{entityId}",
        _ => "#",
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
        DateTime addedAt) => new()
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
        DetailUrl = ResolveDetailUrl(entityType, entityId),
    };
}
