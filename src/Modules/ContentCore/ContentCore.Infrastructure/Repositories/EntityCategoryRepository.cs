using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentCore.Infrastructure.Repositories;

/// <summary>
/// Concrete repository for <see cref="EntityCategory"/> (composite-PK entity).
/// Implements only the minimal contract declared by <see cref="IEntityCategoryRepository"/>.
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

    public void Add(EntityCategory entityCategory)
        => context.Set<EntityCategory>().Add(entityCategory);

    public void Remove(EntityCategory entityCategory)
        => context.Set<EntityCategory>().Remove(entityCategory);
}
