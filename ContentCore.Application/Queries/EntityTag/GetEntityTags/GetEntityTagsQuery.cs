using ContentCore.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.EntityTag.GetEntityTags;

public sealed record EntityTagDto(Guid TagId, string Name, string Slug);

public sealed record GetEntityTagsQuery(string EntityType, Guid EntityId)
    : IQuery<IReadOnlyList<EntityTagDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.EntityTags(EntityType, EntityId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
    public IReadOnlyList<string> Tags => ["entity-tags", $"entity-tags:{EntityType}:{EntityId}"];
}
