using Social.Domain.Entities;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Social.Domain.Repositories;

/// <summary>Repository for <see cref="Favorite"/> aggregate.</summary>
public interface IFavoriteRepository : IRepository<Favorite, Guid>
{
    /// <summary>Returns a user's favorites (cursor-paginated).</summary>
    Task<(IReadOnlyList<Favorite> Items, Guid? NextCursor)> GetByUserPageAsync(
        Guid userId, Guid? afterId, int pageSize, CancellationToken ct = default);

    /// <summary>Returns the specific favorite row if it exists (duplicate check / idempotency).</summary>
    Task<Favorite?> GetByUserAndEntityAsync(
        Guid userId, FavoriteEntityType entityType, Guid entityId, CancellationToken ct = default);

    /// <summary>Returns the count of active (not soft-deleted) favorites for a user.</summary>
    Task<int> CountByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns all favorites referencing the given entity (for orphaned-cleanup service).</summary>
    Task<IReadOnlyList<Favorite>> GetByEntityAsync(
        FavoriteEntityType entityType, Guid entityId, CancellationToken ct = default);
}
