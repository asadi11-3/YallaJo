using Booking.Contracts.Services;
using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuide.Analytics;

public sealed record GetGuidePopularToursQuery(Guid GuideUserId, int Limit = 10)
    : IQuery<IReadOnlyList<PopularTour>>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.PopularTours(GuideUserId, Limit);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForAnalytics(GuideUserId)];
}
