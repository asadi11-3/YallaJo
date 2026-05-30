using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuides.GetMyApplications;

public sealed record GetMyGuideApplicationsQuery(Guid TourGuideId, int Page = 1, int PageSize = 20)
    : IQuery<GetMyGuideApplicationsResult>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.GuideApplications(TourGuideId, Page, PageSize);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForGuideApplications(TourGuideId)];
}

public sealed record GetMyGuideApplicationsResult(
    IReadOnlyList<GuideApplicationListItemDto> Items,
    int TotalCount);

public sealed record GuideApplicationListItemDto(
    Guid ApplicationId,
    Guid TourId,
    string? TourTitle,
    string Status,
    string? Message,
    decimal? ProposedBasePrice,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? RejectionReason);
