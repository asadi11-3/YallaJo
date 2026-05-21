using Social.Domain.Entities;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Social.Domain.Repositories;

/// <summary>Repository for <see cref="EntityRatingCache"/> aggregate.</summary>
public interface IEntityRatingCacheRepository : IRepository<EntityRatingCache, Guid>
{
    /// <summary>Returns the cache entry for a (targetType, targetId) pair, or null if not yet computed.</summary>
    Task<EntityRatingCache?> GetByTargetAsync(
        ReviewTargetType targetType, Guid targetId, CancellationToken ct = default);

    /// <summary>Returns all cache entries that have not been recalculated within the given window (for bulk recalc).</summary>
    Task<IReadOnlyList<EntityRatingCache>> GetStaleAsync(
        DateTime olderThan, CancellationToken ct = default);
}
