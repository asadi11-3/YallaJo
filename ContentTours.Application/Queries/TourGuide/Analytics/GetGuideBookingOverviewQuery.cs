using Booking.Contracts.Services;
using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuide.Analytics;

public sealed record GetGuideBookingOverviewQuery(Guid GuideUserId)
    : IQuery<GuideBookingOverview>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.AnalyticsOverview(GuideUserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForAnalytics(GuideUserId)];
}
