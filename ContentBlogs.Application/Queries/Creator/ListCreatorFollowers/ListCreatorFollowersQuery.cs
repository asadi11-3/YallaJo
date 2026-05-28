using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Creator.ListCreatorFollowers;

/// <summary>
/// Returns paginated follower user IDs for a creator profile.
/// </summary>
public sealed record ListCreatorFollowersQuery(
    Guid CreatorProfileId,
    int Page = 1,
    int PageSize = 20)
    : IQuery<IReadOnlyList<Guid>>, ICacheableQuery
{
    public string CacheKey =>
        ContentBlogsCacheKeys.CreatorFollowerList(CreatorProfileId, Page, PageSize);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags =>
    [
        ContentBlogsCacheKeys.CreatorFollowersTag(CreatorProfileId)
    ];
}
