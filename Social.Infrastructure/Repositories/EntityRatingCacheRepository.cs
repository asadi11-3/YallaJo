using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Social.Infrastructure.Repositories;

internal sealed class EntityRatingCacheRepository(SocialDbContext context)
    : EfRepository<EntityRatingCache, Guid>(context), IEntityRatingCacheRepository
{
    public Task<EntityRatingCache?> GetByTargetAsync(
        ReviewTargetType targetType, Guid targetId, CancellationToken ct = default)
        => context.EntityRatingCaches
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TargetType == targetType && c.TargetId == targetId, ct);

    public async Task<IReadOnlyList<EntityRatingCache>> GetStaleAsync(
        DateTime olderThan, CancellationToken ct = default)
        => await context.EntityRatingCaches
            .AsNoTracking()
            .Where(c => c.LastRecalculatedAt < olderThan)
            .OrderBy(c => c.LastRecalculatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
