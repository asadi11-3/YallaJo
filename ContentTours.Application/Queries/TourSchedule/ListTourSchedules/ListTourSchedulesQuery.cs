using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourSchedule.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourSchedule.ListTourSchedules;

public sealed record ListTourSchedulesQuery(Guid TourId, bool ActiveOnly = true)
    : IQuery<IReadOnlyList<TourScheduleDto>>, ICacheableQuery
{
    public string CacheKey => TourScheduleCacheKeys.List(TourId, ActiveOnly);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags =>
        [TourScheduleCacheKeys.TagForTour(TourId), TourCacheKeys.TagForTour(TourId)];
}
