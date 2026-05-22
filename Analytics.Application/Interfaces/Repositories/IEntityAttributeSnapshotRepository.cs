using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface IEntityAttributeSnapshotRepository : IRepository<EntityAttributeSnapshot, Guid>
{
    Task<EntityAttributeSnapshot?> GetByEntityAsync(EntityType entityKind, Guid entityId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<(EntityType Kind, Guid Id), EntityAttributeSnapshot>> GetByEntitiesAsync(IEnumerable<(EntityType Kind, Guid Id)> keys, CancellationToken ct = default);
    Task<IReadOnlyList<EntityAttributeSnapshot>> GetActiveByKindAsync(EntityType entityKind, CancellationToken ct = default);
    Task<IReadOnlyList<EntityAttributeSnapshot>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<IReadOnlyList<EntityAttributeSnapshot>> GetNearbyAsync(decimal latitude, decimal longitude, double radiusKm, EntityType? kindFilter, int limit, CancellationToken ct = default);
}
