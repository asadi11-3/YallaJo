using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Blog.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Blog.GetBlogBySlug;

public sealed record GetBlogBySlugQuery(string Slug, string? AcceptLanguage = null)
    : IQuery<BlogDetailDto>, ICacheableQuery
{
    public string CacheKey =>
        ContentBlogsCacheKeys.BlogBySlug(NormalizeSlug(Slug), NormalizeLanguage(AcceptLanguage));

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags =>
        [ContentBlogsCacheKeys.BlogSlugTag(Slug)];

    private static string NormalizeSlug(string? slug) =>
        (slug ?? string.Empty).Trim().ToLowerInvariant();

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
