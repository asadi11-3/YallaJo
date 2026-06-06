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

    public async Task<(int ViewCount, int FavoriteCount, int BookingCount, int ReviewCount, decimal AverageRating, int? DaysSinceLastBooking, int InteractionCount)> AggregateForPopularityAsync(EntityType entityType, Guid entityId, DateTime now, CancellationToken ct = default)
    {
        // Aggregates raw inputs for the spec popularity formula (Agents/YallaJo.md §10):
        //   score = bookingCount*3 + reviewCount*2 + avgRating*10 + viewCount*0.1 + favoriteCount*1.5 + recencyBonus
        // We only look at the last 90 days of interactions so stale activity decays naturally.
        var cutoff = now.AddDays(-90);
        var interactions = await context.UserInteractions.AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId && x.OccurredAt >= cutoff)
            .Select(x => new { x.InteractionType, x.OccurredAt })
            .ToListAsync(ct);

        var viewCount = interactions.Count(x => x.InteractionType == InteractionType.View);
        var favoriteCount = interactions.Count(x => x.InteractionType == InteractionType.AddToFavorite);
        var bookingCount = interactions.Count(x => x.InteractionType == InteractionType.BookingCompleted);
        var reviewCount = interactions.Count(x => x.InteractionType == InteractionType.ReviewSubmitted);

        DateTime? lastBooking = interactions
            .Where(x => x.InteractionType == InteractionType.BookingCompleted)
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => (DateTime?)x.OccurredAt)
            .FirstOrDefault();
        int? daysSinceLastBooking = lastBooking is null
            ? null
            : (int)Math.Max(0d, Math.Floor((now - lastBooking.Value).TotalDays));

        var rating = await context.PopularityScores.AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .Select(x => new { x.AverageRatingSnapshot })
            .FirstOrDefaultAsync(ct);
        var averageRating = rating?.AverageRatingSnapshot ?? 0m;

        return (viewCount, favoriteCount, bookingCount, reviewCount, averageRating, daysSinceLastBooking, interactions.Count);
    }
}
