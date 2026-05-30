using Booking.Contracts.Services;
using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuide.Analytics;

public sealed record GetGuideBookingTrendsQuery(Guid GuideUserId, string Granularity = "monthly", int Months = 6)
    : IQuery<IReadOnlyList<BookingTrend>>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.BookingTrends(GuideUserId, Granularity, Months);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForAnalytics(GuideUserId)];
}
