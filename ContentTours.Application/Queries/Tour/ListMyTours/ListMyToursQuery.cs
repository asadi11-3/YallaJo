using ContentTours.Application.Caching;
using ContentTours.Application.Queries.Tour.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.Tour.ListMyTours;

public sealed record ListMyToursQuery(
    Guid EffectiveUserId,
    int Page,
    int PageSize,
    string? StatusFilter,
    string? Sort,
    bool IncludeDeleted)
    : IQuery<ListMyToursResult>, ICacheableQuery
{
    public string CacheKey => TourSearchCacheKeys.MyTours(
        EffectiveUserId, Page, PageSize, StatusFilter, Sort, IncludeDeleted);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
    public IReadOnlyList<string> Tags => [ContentToursCacheKeys.TagForMyTours(EffectiveUserId)];
}

public sealed record ListMyToursResult(
    IReadOnlyList<TourSummaryDto> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);
