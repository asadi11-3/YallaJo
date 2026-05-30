using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.Business.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Business.GetBusinessHours;

public sealed record GetBusinessHoursQuery(
    Guid BusinessId,
    Guid? UserId,
    bool IsAdmin) : IQuery<IReadOnlyList<BusinessHoursDto>>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.BusinessHours(BusinessId, UserId, IsAdmin);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags =>
    [
        ContentPlacesCacheKeys.TagBusinesses,
        ContentPlacesCacheKeys.TagForBusiness(BusinessId),
        ContentPlacesCacheKeys.TagForBusinessHours(BusinessId),
    ];
}
