using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.ServiceItem.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.ServiceItem.ListServiceItems;

/// <remarks>
/// <see cref="IsElevated"/> is computed at the endpoint (business owner OR
/// RolePrivilegeLevel >= Admin) and travels on the query so it participates in
/// the cache key. Two cache entries exist per business — one for the public
/// (IsAvailable=true) view and one for the elevated (all items) view. The tag
/// "biz:{BusinessId}:services" invalidates both on any write.
/// </remarks>
public sealed record ListServiceItemsQuery(Guid BusinessId, bool IsElevated)
    : IQuery<IReadOnlyList<ServiceItemDto>>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.ServiceItemList(BusinessId, IsElevated);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags =>
    [
        ContentPlacesCacheKeys.TagBusinesses,
        ContentPlacesCacheKeys.TagForBusinessServices(BusinessId),
    ];
}
