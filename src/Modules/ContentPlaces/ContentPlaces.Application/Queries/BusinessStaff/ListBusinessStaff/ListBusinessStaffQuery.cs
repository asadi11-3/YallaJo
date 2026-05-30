using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.BusinessStaff.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record ListBusinessStaffQuery(Guid BusinessId)
    : IQuery<IReadOnlyList<BusinessStaffDto>>, ICacheableQuery
{
    public string CacheKey => $"content_places:business_staff:{BusinessId}";

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags =>
    [
        ContentPlacesCacheKeys.TagBusinesses,
        ContentPlacesCacheKeys.TagForBusiness(BusinessId),
    ];
}
