using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Creator.AdminGetApplication;

public sealed record AdminGetCreatorApplicationQuery(Guid ApplicationId)
    : IQuery<CreatorApplicationDto>, ICacheableQuery
{
    public string CacheKey => ContentBlogsCacheKeys.CreatorApplication(ApplicationId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags =>
    [
        ContentBlogsCacheKeys.CreatorApplicationsListTag,
        ContentBlogsCacheKeys.CreatorApplicationTag(ApplicationId)
    ];
}
