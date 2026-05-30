using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourGuides.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourWaypoints.GetByTourId;

public sealed record GetTourWaypointsQuery(Guid TourId)
    : IQuery<IReadOnlyCollection<TourWaypointDto>>, ICacheableQuery
{
    public string CacheKey => TourWaypointCacheKeys.List(TourId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags =>
        [TourWaypointCacheKeys.TagForTour(TourId), ContentToursCacheKeys.TagForTour(TourId)];
}
