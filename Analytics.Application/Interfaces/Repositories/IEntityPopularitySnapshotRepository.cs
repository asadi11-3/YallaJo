using Analytics.Domain.Entities;
using Analytics.Domain.Enums;

namespace Analytics.Application.Interfaces.Repositories;

public interface IEntityPopularitySnapshotRepository
{
    Task<IReadOnlyList<EntityPopularitySnapshot>> GetRecentByEntityAsync(EntityType entityType, Guid entityId, int days, CancellationToken ct = default);
    Task AddBatchAsync(IReadOnlyList<EntityPopularitySnapshot> snapshots, CancellationToken ct = default);
    Task SnapshotCurrentAsync(DateTime takenAt, CancellationToken ct = default);
    Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
}
