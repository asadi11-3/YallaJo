using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourGuides.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuides.ListPublic;

public sealed record ListTourGuidesQuery(int Page = 1, int PageSize = 20)
    : IQuery<ListTourGuidesResult>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.AllGuidesList(Page, PageSize);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagAllGuidesList];
}

public sealed record ListTourGuidesResult(
    IReadOnlyList<TourGuideListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record TourGuideListItemDto(
    Guid Id,
    Guid UserId,
    string? DisplayName,
    string? Slug,
    string? AvatarUrl,
    string Bio,
    decimal AverageRating,
    int ReviewCount,
    int TourCount,
    IReadOnlyList<TourGuideLanguageDto> Languages,
    IReadOnlyList<TourGuideSpecializationDto> Specializations);
