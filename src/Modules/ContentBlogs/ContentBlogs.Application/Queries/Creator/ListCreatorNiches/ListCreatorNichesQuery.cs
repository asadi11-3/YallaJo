using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Creator.ListCreatorNiches;

public sealed record ListCreatorNichesQuery
    : IQuery<IReadOnlyList<CreatorNicheDto>>, ICacheableQuery
{
    public string CacheKey => ContentBlogsCacheKeys.CreatorNicheList();
    public TimeSpan? CacheDuration => TimeSpan.FromHours(1);
    public IReadOnlyList<string> Tags => [ContentBlogsCacheKeys.CreatorNichesTag];
}
