using Analytics.Domain.Entities;
using Analytics.Domain.Repositories;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class PopularityScoreRepository(AnalyticsDbContext context)
    : EfRepository<PopularityScore, Guid>(context), IPopularityScoreRepository
{
    public Task<PopularityScore?> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => context.PopularityScores.FirstOrDefaultAsync(p => p.EntityType == entityType && p.EntityId == entityId, ct);

    public async Task<IReadOnlyList<PopularityScore>> GetTrendingAsync(string entityType, int take, CancellationToken ct = default)
        => await context.PopularityScores
            .Where(p => p.EntityType == entityType)
            .OrderByDescending(p => p.TrendingScore)
            .Take(take)
            .ToListAsync(ct);
}
