using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentCore.Infrastructure.Repositories;

/// <summary>
/// Standalone repository for <see cref="EntityTag"/>.
/// EntityTag uses a composite primary key (EntityType, EntityId, TagId) with no single
/// Guid Id column, so it must NOT extend EfEntityRepository&lt;T, Guid&gt; — the base class
/// assumes a single-column Guid PK and would throw at runtime for this entity.
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

    public async Task<bool> ExistsAsync(
        EntityType entityType, Guid entityId, Guid tagId, CancellationToken ct = default)
        => await context.Set<EntityTag>()
            .AnyAsync(et => et.EntityType == entityType
                && et.EntityId == entityId
                && et.TagId == tagId, ct);

    public void Add(EntityTag entityTag)
        => context.Set<EntityTag>().Add(entityTag);

    public void AddRange(IEnumerable<EntityTag> entityTags)
        => context.Set<EntityTag>().AddRange(entityTags);

    public void Remove(EntityTag entityTag)
        => context.Set<EntityTag>().Remove(entityTag);

    public void RemoveRange(IEnumerable<EntityTag> entityTags)
        => context.Set<EntityTag>().RemoveRange(entityTags);
}
