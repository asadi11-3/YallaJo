namespace YallaJo.Web.Areas.Public.Models.Shared;

/// <summary>
/// Model for the shared wishlist heart button (Areas/Public/Views/Shared/_WishlistButton.cshtml).
/// Rendered as the standard favorite toggle wired to favorites.js (.js-favorite).
/// Guest behavior per UI-UX-WL1: disabled heart with a "log in to save" tooltip.
/// </summary>
public sealed class WishlistButtonVm
{
    /// <summary>Wishlist entity type understood by the toggle endpoint: Tour | Place | Business.</summary>
    public string EntityType { get; init; } = string.Empty;

    public Guid EntityId { get; init; }

    /// <summary>Whether the current user is authenticated (enables the toggle).</summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>Extra CSS classes appended to the button (e.g. "shadow-sm").</summary>
    public string? ExtraClass { get; init; }
}
