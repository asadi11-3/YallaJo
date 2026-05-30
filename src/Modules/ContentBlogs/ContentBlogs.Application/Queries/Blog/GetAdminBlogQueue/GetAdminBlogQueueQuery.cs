using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Blog.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentBlogs.Application.Queries.Blog.GetAdminBlogQueue;

public sealed record GetAdminBlogQueueQuery(int Page = 1, int PageSize = 20, string? AcceptLanguage = null)
    : IQuery<PaginatedResult<BlogSummaryDto>>, ICacheableQuery
{
    public string CacheKey =>
        ContentBlogsCacheKeys.AdminBlogQueue(Page, PageSize);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);

    public IReadOnlyList<string> Tags => [ContentBlogsCacheKeys.AdminBlogQueueTag, ContentBlogsCacheKeys.BlogsListTag];
}
