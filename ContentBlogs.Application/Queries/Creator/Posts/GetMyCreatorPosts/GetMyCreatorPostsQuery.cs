using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentBlogs.Application.Queries.Creator.Posts.GetMyCreatorPosts;

public sealed record GetMyCreatorPostsQuery(
    CreatorPostStatus? StatusFilter,
    CreatorPostType? TypeFilter,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResult<CreatorPostSummaryDto>>, ICacheableQuery
{
    // Cache key uses a placeholder — the handler resolves actual profile for querying.
    // Disable caching for this user-scoped query to prevent cross-user leaks.
    public string CacheKey => "creator-posts-mine-no-cache";
    public TimeSpan? CacheDuration => null;
    public IReadOnlyList<string> Tags => [];
}
