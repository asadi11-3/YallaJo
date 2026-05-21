using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Infrastructure.Repositories;

internal sealed class EntityPopularitySnapshotRepository(AnalyticsDbContext context) : IEntityPopularitySnapshotRepository
{
    public async Task<IReadOnlyList<EntityPopularitySnapshot>> GetRecentByEntityAsync(EntityType entityType, Guid entityId, int days, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 365));
        return await context.EntityPopularitySnapshots.AsNoTracking().Where(x => x.EntityType == entityType && x.EntityId == entityId && x.TakenAt >= cutoff).OrderBy(x => x.TakenAt).ToListAsync(ct);
    }
    public Task AddBatchAsync(IReadOnlyList<EntityPopularitySnapshot> snapshots, CancellationToken ct = default) => context.EntityPopularitySnapshots.AddRangeAsync(snapshots, ct);
    public async Task SnapshotCurrentAsync(DateTime takenAt, CancellationToken ct = default)
    {
        var current = await context.PopularityScores.AsNoTracking().Select(x => EntityPopularitySnapshot.Take(x.EntityType, x.EntityId, x.Score, takenAt)).ToListAsync(ct);
        await context.EntityPopularitySnapshots.AddRangeAsync(current, ct);
    }
    public async Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default) => await context.EntityPopularitySnapshots.Where(x => x.TakenAt < cutoff).ExecuteDeleteAsync(ct);
}
