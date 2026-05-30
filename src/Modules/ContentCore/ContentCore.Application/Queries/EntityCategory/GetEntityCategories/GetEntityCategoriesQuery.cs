using ContentCore.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.EntityCategory.GetEntityCategories;

public sealed record GetEntityCategoriesQuery(string EntityType, Guid EntityId)
    : IQuery<IReadOnlyList<EntityCategoryDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.EntityCategories(EntityType, EntityId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
    // Fine-grained only: commands evict by $"entity-categories:{Type}:{Id}", not by the coarse tag.
    public IReadOnlyList<string> Tags => [ContentCoreCacheKeys.EntityCategoriesTag(EntityType, EntityId)];
}
