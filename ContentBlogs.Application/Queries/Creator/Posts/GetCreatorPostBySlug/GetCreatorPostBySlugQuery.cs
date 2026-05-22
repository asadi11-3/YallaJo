using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Creator.Posts.GetCreatorPostBySlug;

public sealed record GetCreatorPostBySlugQuery(string Slug)
    : IQuery<CreatorPostDto>, ICacheableQuery
{
    public string CacheKey => ContentBlogsCacheKeys.CreatorPost(Slug);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags =>
        [ContentBlogsCacheKeys.CreatorPostTag(Slug)];
}
