using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.BlogComment.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentBlogs.Application.Queries.BlogComment.ListBlogComments;

public sealed record ListBlogCommentsQuery(
    Guid BlogId,
    int Page = 1,
    int PageSize = 20)
    : IQuery<PaginatedResult<BlogCommentDto>>, ICacheableQuery
{
    public string CacheKey => ContentBlogsCacheKeys.BlogComments(BlogId, Page, PageSize);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags =>
        [ContentBlogsCacheKeys.BlogCommentsTag(BlogId)];
}
