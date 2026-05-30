using Social.Application.Caching;
using Social.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Queries.GetMyFavorites;

/// <summary>Returns a cursor-paginated list of the caller's favorites.</summary>
public sealed record GetMyFavoritesQuery(
    Guid UserId,
    Guid? AfterCursor = null,
    int PageSize = 20) : IQuery<FavoritePageDto>, ICacheableQuery
{
    public string CacheKey => $"favorites:mine:{UserId}:{AfterCursor}:{PageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [SocialCacheKeys.FavoritesTag(UserId)];
}
