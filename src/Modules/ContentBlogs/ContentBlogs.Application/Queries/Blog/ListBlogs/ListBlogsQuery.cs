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
    bool? IsFeatured = null,
    string? AcceptLanguage = null)
    : IQuery<PaginatedResult<BlogSummaryDto>>, ICacheableQuery
{

    public string CacheKey
    {
        get
        {
            var filter = $"place:{PlaceId}" +
                         $":search:{NormalizeSearch(Search)}" +
                         $":featured:{NormalizeFeatured(IsFeatured)}" +
                         $":lang:{NormalizeLanguage(AcceptLanguage)}";
            return ContentBlogsCacheKeys.BlogList(filter, Page, PageSize);
        }
    }

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags
    {
        get
        {
            var tags = new List<string> { ContentBlogsCacheKeys.BlogsListTag };
            if (PlaceId.HasValue)
                tags.Add(ContentBlogsCacheKeys.BlogPlaceTag(PlaceId.Value));
            if (IsFeatured.HasValue)
                tags.Add(ContentBlogsCacheKeys.FeaturedBlogsTag);
            return tags;
        }
    }

    private static string NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim().ToLowerInvariant();

    private static string NormalizeFeatured(bool? value) =>
        value switch
        {
            true => "true",
            false => "false",
            null => "any",
        };

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
