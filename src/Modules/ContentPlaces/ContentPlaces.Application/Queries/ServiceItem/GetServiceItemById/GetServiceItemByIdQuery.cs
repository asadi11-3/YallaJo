using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.ServiceItem.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.ServiceItem.GetServiceItemById;

public sealed record GetServiceItemByIdQuery(Guid BusinessId, Guid ServiceItemId)
    : IQuery<ServiceItemDetailDto>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.ServiceItem(ServiceItemId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags =>
    [
        ContentPlacesCacheKeys.TagBusinesses,
        ContentPlacesCacheKeys.TagForServiceItem(ServiceItemId),
    ];
}
