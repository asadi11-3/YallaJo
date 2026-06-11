using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Creator.AdminApplicationStatusCounts;

/// <summary>
/// Admin — per-status creator application counts for the queue's counted tabs.
/// Shares the applications-list cache tag so command-side invalidation covers it.
/// </summary>
public sealed record AdminCreatorApplicationStatusCountsQuery
    : IQuery<CreatorApplicationStatusCountsDto>, ICacheableQuery
{
    public string CacheKey => ContentBlogsCacheKeys.CreatorApplicationStatusCounts;

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(3);

    public IReadOnlyList<string> Tags => [ContentBlogsCacheKeys.CreatorApplicationsListTag];
}
