using ContentTours.Application.Caching;
using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuides.ListApplications;

public sealed record ListGuideApplicationsQuery(
    Guid TourId,
    GuideApplicationStatus? Status = null,
    int Page = 1,
    int PageSize = 20)
    : IQuery<ListGuideApplicationsResult>, ICacheableQuery
{
    public string CacheKey =>
        TourGuideCacheKeys.TourApplications(TourId, Page, PageSize)
        + (Status.HasValue ? $":st{(int)Status.Value}" : string.Empty);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForTourApplications(TourId)];
}

public sealed record ListGuideApplicationsResult(
    IReadOnlyList<GuideApplicationAdminListItemDto> Items,
    int TotalCount);

public sealed record GuideApplicationAdminListItemDto(
    Guid ApplicationId,
    Guid TourId,
    string? TourTitle,
    Guid TourGuideId,
    Guid GuideUserId,
    string Status,
    string? Message,
    string? RelevantExperience,
    decimal? ProposedBasePrice,
    int ResubmissionCount,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedByAdminId,
    string? RejectionReason);
