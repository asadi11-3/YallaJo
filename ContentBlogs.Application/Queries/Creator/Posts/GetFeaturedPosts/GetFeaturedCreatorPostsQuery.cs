using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Creator.Posts.GetFeaturedPosts;

public sealed record GetFeaturedCreatorPostsQuery(int Limit = 12)
    : IQuery<IReadOnlyList<CreatorPostSummaryDto>>, ICacheableQuery
{
    public string CacheKey => $"creators:posts:featured:{Limit}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags =>
        [ContentBlogsCacheKeys.CreatorPostsFeaturedTag];
}
