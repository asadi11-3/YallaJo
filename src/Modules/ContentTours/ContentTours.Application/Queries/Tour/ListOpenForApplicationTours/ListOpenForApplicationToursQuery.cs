using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.Tour.ListOpenForApplicationTours;

/// <summary>
/// Public browse query: tours that are Approved AND open for guide applications.
/// Backs the guide-side tour picker (F10) on the Applications page.
/// </summary>
public sealed record ListOpenForApplicationToursQuery(
    string? Q = null,
    int Page = 1,
    int PageSize = 20) : IQuery<ListOpenForApplicationToursResult>, ICacheableQuery
{
    public string CacheKey => ContentToursCacheKeys.OpenForApplicationTours(Page, PageSize, Q);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags =>
        [ContentToursCacheKeys.TagToursList, ContentToursCacheKeys.TagToursOpenForApplications];
}

public sealed record ListOpenForApplicationToursResult(
    IReadOnlyList<OpenForApplicationTourDto> Items,
    int TotalCount);

public sealed record OpenForApplicationTourDto(
    Guid TourId,
    string Title,
    string? Slug,
    string? City,
    decimal BasePrice,
    string Currency);
