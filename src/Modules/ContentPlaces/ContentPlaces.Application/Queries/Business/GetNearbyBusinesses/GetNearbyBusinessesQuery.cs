using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.Business.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Business.GetNearbyBusinesses;

public sealed record GetNearbyBusinessesQuery(
    double Lat,
    double Lng,
    double RadiusKm = 10,
    int PageSize = 10)
    : IQuery<IReadOnlyList<NearbyBusinessSummaryDto>>, ICacheableQuery
{
    // Coordinates rounded to 4 decimal places (~11m precision) for effective cache reuse.
    public string CacheKey => ContentPlacesCacheKeys.NearbyBusinesses(Lat, Lng, RadiusKm, PageSize);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2); // short TTL — geo data changes
    public IReadOnlyList<string> Tags => [ContentPlacesCacheKeys.TagBusinesses];
}
