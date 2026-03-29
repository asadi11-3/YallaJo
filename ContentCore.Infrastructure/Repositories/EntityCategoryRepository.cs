using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentCore.Infrastructure.Repositories;

/// <summary>
/// Standalone repository for <see cref="EntityCategory"/>.
/// EntityCategory uses a composite primary key (EntityType, EntityId, CategoryId) with no
/// single Guid Id column, so it must NOT extend EfEntityRepository&lt;T, Guid&gt; — the base
/// class assumes a single-column Guid PK and would throw at runtime for this entity.
/// </summary>
internal sealed class EntityCategoryRepository(ContentCoreDbContext context) : IEntityCategoryRepository
{
    public async Task<IReadOnlyList<EntityCategory>> GetByEntityAsync(
        EntityType entityType, Guid entityId, CancellationToken ct = default)
        => await context.Set<EntityCategory>()
            .Include(ec => ec.Category)
            .Where(ec => ec.EntityType == entityType && ec.EntityId == entityId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(
        EntityType entityType, Guid entityId, Guid categoryId, CancellationToken ct = default)
        => await context.Set<EntityCategory>()
            .AnyAsync(ec => ec.EntityType == entityType
                && ec.EntityId == entityId
                && ec.CategoryId == categoryId, ct);

    public void Add(EntityCategory entityCategory)
        => context.Set<EntityCategory>().Add(entityCategory);

    public void AddRange(IEnumerable<EntityCategory> entityCategories)
        => context.Set<EntityCategory>().AddRange(entityCategories);

    public void Remove(EntityCategory entityCategory)
        => context.Set<EntityCategory>().Remove(entityCategory);

    public void RemoveRange(IEnumerable<EntityCategory> entityCategories)
        => context.Set<EntityCategory>().RemoveRange(entityCategories);
}
