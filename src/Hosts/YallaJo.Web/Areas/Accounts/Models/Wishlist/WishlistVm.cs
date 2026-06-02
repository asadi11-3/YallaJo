namespace YallaJo.Web.Areas.Accounts.Models.Wishlist;

public sealed class WishlistVm
{
    public IReadOnlyList<WishlistItemVm> Items { get; init; } = [];
}

public sealed class WishlistItemVm
{
    public string EntityType { get; init; } = string.Empty;

    public Guid EntityId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Subtitle { get; init; }

    public string? ImageUrl { get; init; }

    public decimal? Price { get; init; }

    public string? Currency { get; init; }

    public decimal? Rating { get; init; }

    public DateTime AddedAt { get; init; }

    /// <summary>Friendly label for the entity kind (e.g. "Tour", "Place").</summary>
    public string KindLabel { get; init; } = string.Empty;
}
