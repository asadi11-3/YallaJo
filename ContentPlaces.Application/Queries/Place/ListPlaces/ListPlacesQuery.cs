using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.Place.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentPlaces.Application.Queries.Place.ListPlaces;

public sealed record ListPlacesQuery(
    int Page,
    int PageSize,
    Guid? CategoryId,
    decimal? RatingMin,
    decimal? RatingMax,
    string? City,
    string? Country) : IQuery<PaginatedResult<PlaceSummaryDto>>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.PlaceList(Page, PageSize, CategoryId, City, Country, RatingMin, RatingMax);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags => ["places"];
}
