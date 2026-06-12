using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Blog.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentBlogs.Application.Queries.Blog.GetAdminBlogDrafts;

public sealed record GetAdminBlogDraftsQuery(int Page = 1, int PageSize = 20, string? AcceptLanguage = null)
    : IQuery<PaginatedResult<BlogSummaryDto>>, ICacheableQuery
{
    public string CacheKey =>
        ContentBlogsCacheKeys.AdminBlogDrafts(Page, PageSize);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);

    public IReadOnlyList<string> Tags => [ContentBlogsCacheKeys.AdminBlogDraftsTag, ContentBlogsCacheKeys.BlogsListTag];
}
