using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuides.GetGuideTours;

public sealed record GetGuideToursQuery(Guid TourGuideId, int Page = 1, int PageSize = 20)
    : IQuery<GetGuideToursResult>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.GuideTours(TourGuideId, Page, PageSize);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForGuideTours(TourGuideId)];
}

public sealed record GetGuideToursResult(
    IReadOnlyList<GuideTourListItemDto> Items,
    int TotalCount);

public sealed record GuideTourListItemDto(
    Guid TourId,
    string Title,
    string? Slug,
    bool IsProposer,
    string OfferingStatus,
    bool OffersPrivateTour,
    DateTime? AssignedAt);
