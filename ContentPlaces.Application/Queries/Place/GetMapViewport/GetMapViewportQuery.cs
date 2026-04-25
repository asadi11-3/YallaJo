using ContentPlaces.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Place.GetMapViewport;

public sealed record GetMapViewportQuery(
    double NorthLat,
    double SouthLat,
    double EastLng,
    double WestLng)
    : IQuery<MapViewportResponse>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.MapViewport(NorthLat, SouthLat, EastLng, WestLng);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
    public IReadOnlyList<string> Tags => ["places"];
}
