using Social.Domain.Entities;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Social.Domain.Repositories;

/// <summary>Repository for <see cref="Review"/> aggregate.</summary>
public interface IReviewRepository : IRepository<Review, Guid>
{
    /// <summary>Returns reviews left by a specific user (cursor-paginated).</summary>
    Task<(IReadOnlyList<Review> Items, Guid? NextCursor)> GetByUserPageAsync(
        Guid userId, Guid? afterId, int pageSize, CancellationToken ct = default);

    /// <summary>Returns published/visible reviews for a target entity (cursor-paginated).</summary>
    Task<(IReadOnlyList<Review> Items, Guid? NextCursor)> GetByTargetPageAsync(
        ReviewTargetType targetType, Guid targetId, Guid? afterId, int pageSize, CancellationToken ct = default);

    /// <summary>Returns reviews awaiting moderation or flagged (admin, cursor-paginated).</summary>
    Task<(IReadOnlyList<Review> Items, Guid? NextCursor)> GetFlaggedPageAsync(
        Guid? afterId, int pageSize, CancellationToken ct = default);

    /// <summary>Returns a single review for (userId, targetType, targetId) — for uniqueness checks.</summary>
    Task<Review?> GetByUserAndTargetAsync(
        Guid userId, ReviewTargetType targetType, Guid targetId, CancellationToken ct = default);

    /// <summary>Returns all non-deleted reviews for a given target (used by rating recalculation).</summary>
    Task<IReadOnlyList<Review>> GetPublishedByTargetAsync(
        ReviewTargetType targetType, Guid targetId, CancellationToken ct = default);

    /// <summary>Returns distinct (TargetType, TargetId) pairs that have at least one published review.
    /// Used by <see cref="RatingRecalculationService"/> to enumerate all ratable entities.</summary>
    Task<IReadOnlyList<(ReviewTargetType TargetType, Guid TargetId)>> GetDistinctPublishedTargetsAsync(
        CancellationToken ct = default);
}
