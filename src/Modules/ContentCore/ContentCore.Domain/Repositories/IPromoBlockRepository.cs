using ContentCore.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Domain.Repositories;

/// <summary>
/// Repository contract for <see cref="PromoBlock"/> aggregates.
///
/// Inherits the generic read/write surface via <see cref="IRepository{TEntity,TKey}"/>.
/// A placement-key lookup is added because promo blocks are addressed by their
/// stable slot key rather than by Id in the inline-editing flows.
/// </summary>
public interface IPromoBlockRepository : IRepository<PromoBlock, Guid>
{
    /// <summary>Retrieves a single promo block by its placement key (ignoring scheduling/active state).</summary>
    Task<PromoBlock?> GetByPlacementKeyAsync(string placementKey, CancellationToken ct = default);

    /// <summary>Retrieves all promo blocks for the given placement keys (ignoring scheduling/active state).</summary>
    Task<IReadOnlyList<PromoBlock>> GetByPlacementKeysAsync(
        IReadOnlyCollection<string> placementKeys,
        CancellationToken ct = default);
}
