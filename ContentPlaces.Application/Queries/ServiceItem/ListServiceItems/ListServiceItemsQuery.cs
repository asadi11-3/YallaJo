using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.ServiceItem.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.ServiceItem.ListServiceItems;

/// <remarks>
/// Cache key does NOT vary by caller role because owner/admin can see unavailable items.
/// Public-only results are cached; owner/admin bypass the cache via a separate uncached code path
/// (handled by the handler checking ICurrentUser and skipping cache for elevated callers).
/// For simplicity, we cache only the public (IsAvailable) view; the tag "biz:{BusinessId}:services"
/// ensures both lists are invalidated on any write operation.
/// </remarks>
public sealed record ListServiceItemsQuery(Guid BusinessId)
    : IQuery<IReadOnlyList<ServiceItemDto>>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.ServiceItemList(BusinessId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["businesses", $"biz:{BusinessId}:services"];
}
