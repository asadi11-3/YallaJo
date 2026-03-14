using ContentCore.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.EntityCategory.GetEntityCategories;

public sealed record EntityCategoryDto(Guid CategoryId, string Name, string Slug);

public sealed record GetEntityCategoriesQuery(string EntityType, Guid EntityId)
    : IQuery<IReadOnlyList<EntityCategoryDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.EntityCategories(EntityType, EntityId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
}
