using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class PopularityScoreRepository(AnalyticsDbContext context) : EfRepository<PopularityScore, Guid>(context), IPopularityScoreRepository
{
    public Task<PopularityScore?> GetByEntityAsync(EntityType entityType, Guid entityId, CancellationToken ct = default)
        => context.PopularityScores.FirstOrDefaultAsync(x => x.EntityType == entityType && x.EntityId == entityId, ct);
    public async Task<IReadOnlyList<PopularityScore>> GetTopByTypeAsync(EntityType entityType, int count, CancellationToken ct = default)
        => await context.PopularityScores.AsNoTracking().Where(x => x.EntityType == entityType).OrderByDescending(x => x.Score).Take(Math.Clamp(count, 1, 100)).ToListAsync(ct);
    public async Task<IReadOnlyList<PopularityScore>> GetTrendingAsync(EntityType entityType, int limit, CancellationToken ct = default)
        => await context.PopularityScores.AsNoTracking().Where(x => x.EntityType == entityType && x.TrendingRank != null).OrderBy(x => x.TrendingRank).Take(Math.Clamp(limit, 1, 100)).ToListAsync(ct);
    public async Task<IReadOnlyList<PopularityScore>> GetStaleAsync(int batchSize, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-1);
        return await context.PopularityScores.Where(x => x.IsStale || x.LastRecalculatedAt == null || x.LastRecalculatedAt < cutoff).OrderByDescending(x => x.IsStale).ThenBy(x => x.LastRecalculatedAt).Take(Math.Clamp(batchSize, 1, 1000)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PopularityScore>> GetAllByTypeAsync(EntityType entityType, CancellationToken ct = default)
        => await context.PopularityScores.Where(x => x.EntityType == entityType).ToListAsync(ct);

    public async Task<IReadOnlyList<PopularityScore>> GetAllAsync(CancellationToken ct = default)
        => await context.PopularityScores.ToListAsync(ct);
}
