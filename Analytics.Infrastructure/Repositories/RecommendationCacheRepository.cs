using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class RecommendationCacheRepository(AnalyticsDbContext context) : EfRepository<RecommendationCache, Guid>(context), IRecommendationCacheRepository
{
    public async Task<IReadOnlyList<RecommendationCache>> GetByBatchIdAsync(Guid batchId, CancellationToken ct = default)
        => await context.RecommendationCaches
            .AsNoTracking()
            .Where(x => x.BatchId == batchId)
            .Where(x => x.ExpiresAt > DateTime.UtcNow)
            .OrderBy(x => x.Position)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RecommendationCache>> GetByUserAsync(Guid userId, int limit, CancellationToken ct = default)
        => await context.RecommendationCaches
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Position)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);

    public async Task DeleteByBatchIdAsync(Guid batchId, CancellationToken ct = default)
{
    var rows = await context.RecommendationCaches
        .Where(x => x.BatchId == batchId)
        .ToListAsync(ct);

    context.RecommendationCaches.RemoveRange(rows);
}
}
