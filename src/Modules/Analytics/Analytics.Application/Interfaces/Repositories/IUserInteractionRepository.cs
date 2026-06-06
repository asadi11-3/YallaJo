using Analytics.Domain.Entities;
using Analytics.Domain.Enums;

namespace Analytics.Application.Interfaces.Repositories;

public interface IUserInteractionRepository
{
    Task AddAsync(UserInteraction interaction, CancellationToken ct = default);
    Task AddBatchAsync(IReadOnlyList<UserInteraction> interactions, CancellationToken ct = default);
    Task<(IReadOnlyList<UserInteraction> Items, long? NextId)> GetPageAsync(Guid? userId, EntityType? entityType, Guid? entityId, InteractionType? type, DateTime? from, DateTime? to, long? afterId, int pageSize, CancellationToken ct = default);
    /// <summary>
    /// Aggregates raw popularity inputs (per spec §10) from the last 90 days for the given entity:
    /// view, favorite, completed-booking, and review counts; the snapshot average rating; days
    /// since the last completed booking (null if none); and the total interaction count.
    /// </summary>
    Task<(int ViewCount, int FavoriteCount, int BookingCount, int ReviewCount, decimal AverageRating, int? DaysSinceLastBooking, int InteractionCount)> AggregateForPopularityAsync(EntityType entityType, Guid entityId, DateTime now, CancellationToken ct = default);
}
