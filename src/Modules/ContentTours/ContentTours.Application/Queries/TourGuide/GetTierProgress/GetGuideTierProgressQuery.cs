using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuide.GetTierProgress;

public sealed record GuideTierProgressDto(
    string CurrentTier,
    string? NextTier,
    int CompletedTours,
    int CompletedToursRequired,
    decimal AverageRating,
    decimal AverageRatingRequired,
    decimal ReportRate,
    decimal MaxReportRateAllowed,
    int ActiveMonths,
    int ActiveMonthsRequired,
    decimal CurrentCommissionRate);

public sealed record GetGuideTierProgressQuery(Guid GuideUserId)
    : IQuery<GuideTierProgressDto>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.TierProgress(GuideUserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForTierProgress(GuideUserId)];
}
