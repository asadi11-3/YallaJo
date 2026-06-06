using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Social.Infrastructure.Repositories;

internal sealed class AccessibilityReviewRepository(SocialDbContext context)
    : EfRepository<AccessibilityReview, Guid>(context), IAccessibilityReviewRepository
{
    public async Task<(IReadOnlyList<AccessibilityReview> Items, Guid? NextCursor)> GetByUserPageAsync(
        Guid userId, Guid? afterId, int pageSize, CancellationToken ct = default)
    {
        var size = Math.Clamp(pageSize, 1, 50);
        var query = context.AccessibilityReviews
            .AsNoTracking()
            .Where(r => r.UserId == userId);

        if (afterId.HasValue)
            query = query.Where(r => r.Id.CompareTo(afterId.Value) < 0);

        var items = await query
            .OrderByDescending(r => r.Id)
            .Take(size + 1)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        Guid? nextCursor = null;
        if (items.Count > size)
        {
            nextCursor = items[size].Id;
            items = items.Take(size).ToList();
        }
        return (items, nextCursor);
    }

    public async Task<(IReadOnlyList<AccessibilityReview> Items, int TotalCount)> GetPublicListAsync(
        ReviewTargetType targetType, Guid targetId, int page, int pageSize, CancellationToken ct = default)
    {
        var pageNumber = Math.Max(page, 1);
        var size = Math.Clamp(pageSize, 1, 50);
        var query = context.AccessibilityReviews
            .AsNoTracking()
            .Where(r => r.TargetType == targetType
                        && r.TargetId == targetId
                        && r.Status == AccessibilityReviewStatus.Published);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public Task<AccessibilityReview?> GetByUserAndTargetAsync(
        Guid userId, ReviewTargetType targetType, Guid targetId, CancellationToken ct = default)
        => context.AccessibilityReviews
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId
                                      && r.TargetType == targetType
                                      && r.TargetId == targetId, ct);
}
