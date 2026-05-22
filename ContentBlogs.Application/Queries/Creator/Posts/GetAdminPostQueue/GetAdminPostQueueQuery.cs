using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentBlogs.Application.Queries.Creator.Posts.GetAdminPostQueue;

public sealed record GetAdminPostQueueQuery(
    CreatorPostStatus? StatusFilter,
    CreatorPostType? TypeFilter,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResult<CreatorPostSummaryDto>>, ICacheableQuery
{
    public string CacheKey =>
        $"admin:posts:queue:status:{StatusFilter}:type:{TypeFilter}:{Page}:{PageSize}";

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(1);
    public IReadOnlyList<string> Tags => [ContentBlogsCacheKeys.AdminPostsQueueTag];
}
