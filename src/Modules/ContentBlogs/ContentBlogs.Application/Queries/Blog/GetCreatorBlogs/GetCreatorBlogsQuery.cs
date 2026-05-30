using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Blog.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentBlogs.Application.Queries.Blog.GetCreatorBlogs;

public sealed record GetCreatorBlogsQuery(
    Guid CreatorProfileId,
    int Page = 1,
    int PageSize = 20,
    string? AcceptLanguage = null)
    : IQuery<PaginatedResult<BlogSummaryDto>>, ICacheableQuery
{
    public string CacheKey =>
        ContentBlogsCacheKeys.CreatorArticleList(CreatorProfileId, "published", Page, PageSize);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags => [ContentBlogsCacheKeys.CreatorArticlesTag(CreatorProfileId)];
}
