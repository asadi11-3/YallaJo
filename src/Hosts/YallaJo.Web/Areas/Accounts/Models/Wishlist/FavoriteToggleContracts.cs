namespace YallaJo.Web.Areas.Accounts.Models.Wishlist;

/// <summary>Entity kinds that can be favorited. Byte values MUST match the backend
/// Social.Domain.Enums.FavoriteEntityType so the value serializes correctly
/// (the web ApiClient has no JsonStringEnumConverter, so enums are sent as numbers).</summary>
public enum FavoriteEntityType : byte
{
    Tour = 0,
    Place = 1,
    Business = 2,
    Blog = 3,
    TourGuide = 4,
}

/// <summary>Request body for POST /api/v1/social/favorites.</summary>
public sealed record AddFavoriteRequest(FavoriteEntityType EntityType, Guid EntityId);

/// <summary>Response body for GET /api/v1/social/favorites/check/{entityType}/{entityId}.</summary>
public sealed record CheckFavoriteResponse(bool IsFavorited);
