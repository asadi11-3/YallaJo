using ContentCore.Application.Caching;
using ContentCore.Application.Queries.Tag.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Tag.GetTagById;

public sealed record GetTagByIdQuery(Guid Id)
    : IQuery<TagDto>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.Tag(Id);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["tags", $"tag:{Id}"];
}
