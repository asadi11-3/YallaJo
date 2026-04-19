using ContentCore.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.EntityTag.GetEntityTags;

public sealed record GetEntityTagsQuery(string EntityType, Guid EntityId)
    : IQuery<IReadOnlyList<EntityTagDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.EntityTags(EntityType, EntityId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
    // Fine-grained only: commands evict by $"entity-tags:{Type}:{Id}", not by the coarse tag.
    public IReadOnlyList<string> Tags => [$"entity-tags:{EntityType}:{EntityId}"];
}
