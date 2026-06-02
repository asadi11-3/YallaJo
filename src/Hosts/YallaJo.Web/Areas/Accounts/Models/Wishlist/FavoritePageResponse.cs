namespace YallaJo.Web.Areas.Accounts.Models.Wishlist;

public sealed class FavoritePageResponse
{
    public IReadOnlyList<FavoriteResponse> Items { get; init; } = [];

    public Guid? NextCursor { get; init; }
}

public sealed class FavoriteResponse
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string EntityType { get; init; } = string.Empty;

    public Guid EntityId { get; init; }

    public DateTime AddedAt { get; init; }
}
