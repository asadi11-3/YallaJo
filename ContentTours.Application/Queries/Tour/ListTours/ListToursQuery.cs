using ContentTours.Application.Caching;
using ContentTours.Application.Queries.Tour.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentTours.Application.Queries.Tour.ListTours;

public sealed record ListToursQuery(
    int Page = 1,
    int PageSize = 20,
    string? Sort = null,
    Guid? PlaceId = null,
    bool? IsFeatured = null,
    string? AcceptLanguage = null) : IQuery<PaginatedResult<TourSummaryDto>>, ICacheableQuery
{
    public string CacheKey =>
        ContentToursCacheKeys.TourList(
            Page,
            PageSize,
            Sort,
            status: null,
            PlaceId,
            IsFeatured,
            AcceptLanguage,
            isElevated: false);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags => IsFeatured == true
        ? [ContentToursCacheKeys.TagToursList, ContentToursCacheKeys.TagToursSearch, ContentToursCacheKeys.TagToursFeatured]
        : [ContentToursCacheKeys.TagToursList, ContentToursCacheKeys.TagToursSearch];
}
