using Social.Application.Caching;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Queries.CheckFavorite;

/// <summary>Returns whether the caller has favorited a given entity.</summary>
public sealed record CheckFavoriteQuery(
    Guid UserId,
    FavoriteEntityType EntityType,
    Guid EntityId) : IQuery<bool>, ICacheableQuery
{
    public string CacheKey => $"favorites:check:{UserId}:{EntityType}:{EntityId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [SocialCacheKeys.FavoritesTag(UserId)];
}
