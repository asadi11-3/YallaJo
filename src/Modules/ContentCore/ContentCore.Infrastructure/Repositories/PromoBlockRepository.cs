using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentCore.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for <see cref="PromoBlock"/> aggregates.
/// Delegates the full read/write surface to <see cref="EfRepository{TEntity,TKey}"/> and
/// isolates EF Core query APIs to this Infrastructure class so Application handlers
/// stay free of Microsoft.EntityFrameworkCore references.
/// </summary>
internal sealed class PromoBlockRepository(ContentCoreDbContext context)
    : EfRepository<PromoBlock, Guid>(context), IPromoBlockRepository
{
    /// <inheritdoc />
    public Task<PromoBlock?> GetByPlacementKeyAsync(string placementKey, CancellationToken ct = default)
        => context.PromoBlocks
            .FirstOrDefaultAsync(x => x.PlacementKey == placementKey, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PromoBlock>> GetByPlacementKeysAsync(
        IReadOnlyCollection<string> placementKeys, CancellationToken ct = default)
    {
        if (placementKeys is null || placementKeys.Count == 0)
            return [];

        return await context.PromoBlocks
            .Where(x => placementKeys.Contains(x.PlacementKey))
            .ToListAsync(ct);
    }
}
