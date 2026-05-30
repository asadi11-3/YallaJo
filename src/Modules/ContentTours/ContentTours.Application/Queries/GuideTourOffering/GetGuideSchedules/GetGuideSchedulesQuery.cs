using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.GuideTourOffering.GetGuideSchedules;

public sealed record GuideScheduleDto(
    Guid Id, byte DayOfWeek, TimeOnly StartTime, TimeOnly? EndTime, bool IsActive, DateTime CreatedAt);

public sealed record GetGuideSchedulesQuery(Guid TourId, Guid TourGuideId)
    : IQuery<IReadOnlyList<GuideScheduleDto>>, ICacheableQuery
{
    public string CacheKey => $"guide-schedules:{TourId}:{TourGuideId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForTourOfferings(TourId)];
}
