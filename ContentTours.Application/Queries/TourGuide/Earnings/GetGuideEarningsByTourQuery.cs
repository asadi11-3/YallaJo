using ContentTours.Application.Caching;
using Finance.Contracts.Services;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuide.Earnings;

public sealed record GetGuideEarningsByTourQuery(Guid GuideUserId)
    : IQuery<IReadOnlyList<GuideEarningByTour>>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.EarningsByTour(GuideUserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForEarnings(GuideUserId)];
}
