using Booking.Contracts.Services;
using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuide.Analytics;

public sealed record GetGuidePeakDaysQuery(Guid GuideUserId)
    : IQuery<IReadOnlyList<PeakDayStat>>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.PeakDays(GuideUserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForAnalytics(GuideUserId)];
}
