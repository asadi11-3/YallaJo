using Social.Domain.Entities;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Social.Domain.Repositories;

/// <summary>Repository for the <see cref="AccessibilityReview"/> aggregate (Phase-3 WS-2, G1).</summary>
public interface IAccessibilityReviewRepository : IRepository<AccessibilityReview, Guid>
{
    /// <summary>Returns the caller's accessibility reviews (cursor-paginated).</summary>
    Task<(IReadOnlyList<AccessibilityReview> Items, Guid? NextCursor)> GetByUserPageAsync(
        Guid userId, Guid? afterId, int pageSize, CancellationToken ct = default);

    /// <summary>Returns published accessibility reviews for a target entity (offset-paginated).</summary>
    Task<(IReadOnlyList<AccessibilityReview> Items, int TotalCount)> GetPublicListAsync(
        ReviewTargetType targetType, Guid targetId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Uniqueness check — S-AR1.</summary>
    Task<AccessibilityReview?> GetByUserAndTargetAsync(
        Guid userId, ReviewTargetType targetType, Guid targetId, CancellationToken ct = default);
}
