using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourGuides.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuides;

public sealed record GetTourGuidesQuery(Guid TourId)
    : IQuery<IReadOnlyCollection<TourGuideDto>>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.List(TourId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags =>
        [TourGuideCacheKeys.TagForTour(TourId), TourCacheKeys.TagForTour(TourId)];
}
