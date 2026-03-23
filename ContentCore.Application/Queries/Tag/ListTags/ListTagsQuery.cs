using ContentCore.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Tag.ListTags;

public sealed record TagDto(Guid Id, string Name, string Slug, bool IsActive);

public sealed record ListTagsQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<TagDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.Tags(ActiveOnly);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => ["tags"];
}
