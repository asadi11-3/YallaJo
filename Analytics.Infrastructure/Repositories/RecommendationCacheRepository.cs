using Analytics.Domain.Entities;
using Analytics.Domain.Repositories;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class RecommendationCacheRepository(AnalyticsDbContext context)
    : EfRepository<RecommendationCache, Guid>(context), IRecommendationCacheRepository
{
    public async Task<IReadOnlyList<RecommendationCache>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.RecommendationCaches
            .Where(r => r.UserId == userId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RecommendationCache>> GetExpiredAsync(DateTime threshold, CancellationToken ct = default)
        => await context.RecommendationCaches
            .Where(r => r.ExpiresAt < threshold)
            .ToListAsync(ct);
}
