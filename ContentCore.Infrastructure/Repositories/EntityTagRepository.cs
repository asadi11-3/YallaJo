using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentCore.Infrastructure.Repositories;

/// <summary>
/// Concrete repository for <see cref="EntityTag"/> (composite-PK entity).
/// Implements only the minimal contract declared by <see cref="IEntityTagRepository"/>.
/// </summary>
internal sealed class EntityTagRepository(ContentCoreDbContext context) : IEntityTagRepository
{
    public async Task<IReadOnlyList<EntityTag>> GetByEntityAsync(
        EntityType entityType, Guid entityId, CancellationToken ct = default)
        => await context.Set<EntityTag>()
            .Include(et => et.Tag)
            .Where(et => et.EntityType == entityType && et.EntityId == entityId)
            .AsNoTracking()
            .ToListAsync(ct);

    public void Add(EntityTag entityTag)
        => context.Set<EntityTag>().Add(entityTag);

    public void Remove(EntityTag entityTag)
        => context.Set<EntityTag>().Remove(entityTag);
}
