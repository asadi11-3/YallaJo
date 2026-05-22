using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Infrastructure.Repositories;

internal sealed class UserInteractionRepository(AnalyticsDbContext context) : IUserInteractionRepository
{
    public Task AddAsync(UserInteraction interaction, CancellationToken ct = default) => context.UserInteractions.AddAsync(interaction, ct).AsTask();
    public Task AddBatchAsync(IReadOnlyList<UserInteraction> interactions, CancellationToken ct = default) => context.UserInteractions.AddRangeAsync(interactions, ct);

    public async Task<(IReadOnlyList<UserInteraction> Items, long? NextId)> GetPageAsync(Guid? userId, EntityType? entityType, Guid? entityId, InteractionType? type, DateTime? from, DateTime? to, long? afterId, int pageSize, CancellationToken ct = default)
    {
        var size = Math.Clamp(pageSize, 1, 50);
        var q = context.UserInteractions.AsNoTracking().AsQueryable();
        if (userId is not null) q = q.Where(x => x.UserId == userId);
        if (entityType is not null) q = q.Where(x => x.EntityType == entityType);
        if (entityId is not null) q = q.Where(x => x.EntityId == entityId);
        if (type is not null) q = q.Where(x => x.InteractionType == type);
        if (from is not null) q = q.Where(x => x.OccurredAt >= from);
        if (to is not null) q = q.Where(x => x.OccurredAt <= to);
        if (afterId is not null) q = q.Where(x => x.Id < afterId);
        var items = await q.OrderByDescending(x => x.Id).Take(size + 1).ToListAsync(ct);
        var next = items.Count > size ? items[^1].Id : (long?)null;
        return (items.Take(size).ToList(), next);
    }

    public async Task<(decimal ViewScore, decimal ClickScore, decimal FavoriteScore, decimal BookingStartedScore, decimal BookingCompletedScore, decimal ReviewScore, int InteractionCount)> AggregateScoreAsync(EntityType entityType, Guid entityId, DateTime now, CancellationToken ct = default)
    {
        var cutoff = now.AddDays(-90);
        var interactions = await context.UserInteractions.AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId && x.OccurredAt >= cutoff)
            .Select(x => new { x.InteractionType, x.OccurredAt })
            .ToListAsync(ct);

        decimal ScoreFor(InteractionType type, decimal weight) => interactions
            .Where(x => x.InteractionType == type)
            .Sum(x => weight * (decimal)Math.Pow(0.5d, Math.Max(0d, (now - x.OccurredAt).TotalDays) / 30d));

        var rating = await context.PopularityScores.AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .Select(x => new { x.AverageRatingSnapshot, x.ReviewCountSnapshot })
            .FirstOrDefaultAsync(ct);
        var multiplier = rating is { ReviewCountSnapshot: >= 3, AverageRatingSnapshot: not null }
            ? 1m + rating.AverageRatingSnapshot.Value / 10m
            : 1m;

        return (
            ScoreFor(InteractionType.View, 1m) * multiplier,
            ScoreFor(InteractionType.Click, 2m) * multiplier,
            ScoreFor(InteractionType.AddToFavorite, 5m) * multiplier,
            ScoreFor(InteractionType.BookingStarted, 8m) * multiplier,
            ScoreFor(InteractionType.BookingCompleted, 15m) * multiplier,
            ScoreFor(InteractionType.ReviewSubmitted, 4m) * multiplier,
            interactions.Count);
    }
}
