using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Creator.ListCreatorFollowers;

/// <summary>
/// Returns paginated, public-safe follower summaries for a creator profile
/// (Gap 3 Phase A). No follower user IDs are exposed — only an opaque ordinal and
/// the followed-at timestamp.
/// </summary>
public sealed record ListCreatorFollowersQuery(
    Guid CreatorProfileId,
    int Page = 1,
    int PageSize = 20)
    : IQuery<IReadOnlyList<FollowerSummaryDto>>, ICacheableQuery
{
    public string CacheKey =>
        ContentBlogsCacheKeys.CreatorFollowerList(CreatorProfileId, Page, PageSize);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags =>
    [
        ContentBlogsCacheKeys.CreatorFollowersTag(CreatorProfileId)
    ];
}
