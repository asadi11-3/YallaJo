using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Social.Infrastructure.Repositories;

internal sealed class ReviewRepository(SocialDbContext context)
    : EfRepository<Review, Guid>(context), IReviewRepository
{
    public async Task<(IReadOnlyList<Review> Items, Guid? NextCursor)> GetByUserPageAsync(
        Guid userId, Guid? afterId, int pageSize, CancellationToken ct = default)
    {
        var size = Math.Clamp(pageSize, 1, 50);
        var query = context.Reviews
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

    public async Task<(IReadOnlyList<Review> Items, Guid? NextCursor)> GetByTargetPageAsync(
        ReviewTargetType targetType, Guid targetId, Guid? afterId, int pageSize, CancellationToken ct = default)
    {
        var size = Math.Clamp(pageSize, 1, 50);
        var query = context.Reviews
            .AsNoTracking()
            .Where(r => r.TargetType == targetType && r.TargetId == targetId
                        && r.Status == ReviewStatus.Published);

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

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetPublicListAsync(
        ReviewTargetType targetType, Guid targetId, int page, int pageSize, CancellationToken ct = default)
    {
        var pageNumber = Math.Max(page, 1);
        var size = Math.Clamp(pageSize, 1, 50);
        var query = context.Reviews
            .AsNoTracking()
            .Include(r => r.Replies)
            .Where(r => r.TargetType == targetType
                        && r.TargetId == targetId
                        && r.Status == ReviewStatus.Published);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(r => r.HelpfulVoteCount)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task<decimal> GetAverageRatingAsync(
        ReviewTargetType targetType, Guid targetId, CancellationToken ct = default)
    {
        var query = context.Reviews
            .AsNoTracking()
            .Where(r => r.TargetType == targetType
                        && r.TargetId == targetId
                        && r.Status == ReviewStatus.Published);

        return await query.AnyAsync(ct).ConfigureAwait(false)
            ? await query.AverageAsync(r => r.Rating, ct).ConfigureAwait(false)
            : 0m;
    }

    public async Task<(IReadOnlyList<Review> Items, Guid? NextCursor)> GetFlaggedPageAsync(
        Guid? afterId, int pageSize, CancellationToken ct = default)
    {
        var size = Math.Clamp(pageSize, 1, 50);
        var query = context.Reviews
            .AsNoTracking()
            .Where(r => r.Status == ReviewStatus.AwaitingModeration
                        || r.Status == ReviewStatus.AutoHidden
                        || r.ProfanityFlagged);

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

    public Task<Review?> GetByUserAndTargetAsync(
        Guid userId, ReviewTargetType targetType, Guid targetId, CancellationToken ct = default)
        => context.Reviews
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId
                                      && r.TargetType == targetType
                                      && r.TargetId == targetId, ct);

    public async Task<IReadOnlyList<Review>> GetPublishedByTargetAsync(
        ReviewTargetType targetType, Guid targetId, CancellationToken ct = default)
        => await context.Reviews
            .AsNoTracking()
            .Where(r => r.TargetType == targetType
                        && r.TargetId == targetId
                        && r.Status == ReviewStatus.Published)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<(ReviewTargetType TargetType, Guid TargetId)>> GetDistinctPublishedTargetsAsync(
        CancellationToken ct = default)
    {
        var pairs = await context.Reviews
            .AsNoTracking()
            .Where(r => r.Status == ReviewStatus.Published)
            .Select(r => new { r.TargetType, r.TargetId })
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return pairs.Select(x => (x.TargetType, x.TargetId)).ToList();
    }
}
