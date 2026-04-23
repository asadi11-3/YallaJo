using ContentCore.Application.Caching;
using ContentCore.Application.Queries.Tag.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Tag.ListTags;

public sealed record ListTagsQuery(bool ActiveOnly = false, bool WithTranslations = false)
    : IQuery<IReadOnlyList<TagDto>>, ICacheableQuery
{
    public string CacheKey => $"{ContentCoreCacheKeys.Tags(ActiveOnly)}:{WithTranslations}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => ["tags"];
}
