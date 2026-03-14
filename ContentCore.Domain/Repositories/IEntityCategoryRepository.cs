using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;

namespace ContentCore.Domain.Repositories;

public interface IEntityCategoryRepository
{
    Task<IReadOnlyList<EntityCategory>> GetByEntityAsync(
        EntityType entityType, Guid entityId, CancellationToken ct = default);

    Task<bool> ExistsAsync(
        EntityType entityType, Guid entityId, Guid categoryId, CancellationToken ct = default);

    void Add(EntityCategory entityCategory);
    void AddRange(IEnumerable<EntityCategory> entityCategories);

    void Remove(EntityCategory entityCategory);
    void RemoveRange(IEnumerable<EntityCategory> entityCategories);
}
