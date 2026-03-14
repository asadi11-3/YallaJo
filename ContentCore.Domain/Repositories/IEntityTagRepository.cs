using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;

namespace ContentCore.Domain.Repositories;

public interface IEntityTagRepository
{
    Task<IReadOnlyList<EntityTag>> GetByEntityAsync(
        EntityType entityType, Guid entityId, CancellationToken ct = default);

    Task<bool> ExistsAsync(
        EntityType entityType, Guid entityId, Guid tagId, CancellationToken ct = default);

    void Add(EntityTag entityTag);
    void AddRange(IEnumerable<EntityTag> entityTags);

    void Remove(EntityTag entityTag);
    void RemoveRange(IEnumerable<EntityTag> entityTags);
}
