using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Creator.GetCreatorProfileBySlug;

public sealed record GetCreatorProfileBySlugQuery(string Slug)
    : IQuery<CreatorProfileDto>, ICacheableQuery
{
    public string CacheKey => ContentBlogsCacheKeys.CreatorProfileBySlug(Slug);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags =>
    [
        ContentBlogsCacheKeys.CreatorProfileSlugTag(Slug)
    ];
}
