using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentCore.Infrastructure.Repositories;

internal sealed class EntityTagRepository(ContentCoreDbContext context) : IEntityTagRepository
{
    public async Task<IReadOnlyList<EntityTag>> GetByEntityAsync(
        EntityType entityType, Guid entityId, CancellationToken ct = default)
        => await context.EntityTags
            .Include(et => et.Tag)
            .Where(et => et.EntityType == entityType && et.EntityId == entityId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(
        EntityType entityType, Guid entityId, Guid tagId, CancellationToken ct = default)
        => await context.EntityTags
            .AnyAsync(et => et.EntityType == entityType
                && et.EntityId == entityId
                && et.TagId == tagId, ct);

    public void Add(EntityTag entityTag)
        => context.EntityTags.Add(entityTag);

    public void AddRange(IEnumerable<EntityTag> entityTags)
        => context.EntityTags.AddRange(entityTags);

    public void Remove(EntityTag entityTag)
        => context.EntityTags.Remove(entityTag);

    public void RemoveRange(IEnumerable<EntityTag> entityTags)
        => context.EntityTags.RemoveRange(entityTags);
}
