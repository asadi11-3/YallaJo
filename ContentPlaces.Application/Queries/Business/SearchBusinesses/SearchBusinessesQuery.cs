using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.Business.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentPlaces.Application.Queries.Business.SearchBusinesses;

public sealed record SearchBusinessesQuery(
    int Page,
    int PageSize,
    string? Query,
    string? BusinessType,
    string? City,
    string? Country) : IQuery<PaginatedResult<BusinessSummaryDto>>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.BusinessSearch(Page, PageSize, Query, BusinessType, City, Country);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [ContentPlacesCacheKeys.TagBusinesses];
}
