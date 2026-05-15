using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Blog.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentBlogs.Application.Queries.Blog.ListBlogs;

public sealed record ListBlogsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? PlaceId = null,
    string? Search = null,
    string? AcceptLanguage = null)
    : IQuery<PaginatedResult<BlogSummaryDto>>, ICacheableQuery
{

    public string CacheKey
    {
        get
        {
            var filter = $"place:{PlaceId}:search:{NormalizeSearch(Search)}:lang:{NormalizeLanguage(AcceptLanguage)}";
            return ContentBlogsCacheKeys.BlogList(filter, Page, PageSize);
        }
    }

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags => PlaceId.HasValue
        ? [ContentBlogsCacheKeys.BlogsListTag, ContentBlogsCacheKeys.BlogPlaceTag(PlaceId.Value)]
        : [ContentBlogsCacheKeys.BlogsListTag];

    private static string NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim().ToLowerInvariant();

    private static string NormalizeLanguage(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return "default";

        var first = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(first))
            return "default";

        return first
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0]
            .Trim()
            .ToLowerInvariant();
    }
}
