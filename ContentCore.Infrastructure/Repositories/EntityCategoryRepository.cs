using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentCore.Infrastructure.Repositories;

internal sealed class EntityCategoryRepository(ContentCoreDbContext context) : IEntityCategoryRepository
{
    public async Task<IReadOnlyList<EntityCategory>> GetByEntityAsync(
        EntityType entityType, Guid entityId, CancellationToken ct = default)
        => await context.EntityCategories
            .Include(ec => ec.Category)
            .Where(ec => ec.EntityType == entityType && ec.EntityId == entityId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(
        EntityType entityType, Guid entityId, Guid categoryId, CancellationToken ct = default)
        => await context.EntityCategories
            .AnyAsync(ec => ec.EntityType == entityType
                && ec.EntityId == entityId
                && ec.CategoryId == categoryId, ct);

    public void Add(EntityCategory entityCategory)
        => context.EntityCategories.Add(entityCategory);

    public void AddRange(IEnumerable<EntityCategory> entityCategories)
        => context.EntityCategories.AddRange(entityCategories);

    public void Remove(EntityCategory entityCategory)
        => context.EntityCategories.Remove(entityCategory);

    public void RemoveRange(IEnumerable<EntityCategory> entityCategories)
        => context.EntityCategories.RemoveRange(entityCategories);
}
