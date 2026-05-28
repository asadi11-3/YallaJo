using ContentTours.Application.Caching;
using Finance.Contracts.Services;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentTours.Application.Queries.TourGuide.Earnings;

public sealed record GetGuideEarningsHistoryQuery(Guid GuideUserId, int Page = 1, int PageSize = 20)
    : IQuery<PaginatedResult<GuideEarningHistoryItem>>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.EarningsHistory(GuideUserId, Page, PageSize);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForEarnings(GuideUserId)];
}
