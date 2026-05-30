using Analytics.Domain.Entities;
using Analytics.Domain.Enums;

namespace Analytics.Application.Interfaces.Repositories;

public interface IUserInteractionRepository
{
    Task AddAsync(UserInteraction interaction, CancellationToken ct = default);
    Task AddBatchAsync(IReadOnlyList<UserInteraction> interactions, CancellationToken ct = default);
    Task<(IReadOnlyList<UserInteraction> Items, long? NextId)> GetPageAsync(Guid? userId, EntityType? entityType, Guid? entityId, InteractionType? type, DateTime? from, DateTime? to, long? afterId, int pageSize, CancellationToken ct = default);
    Task<(decimal ViewScore, decimal ClickScore, decimal FavoriteScore, decimal BookingStartedScore, decimal BookingCompletedScore, decimal ReviewScore, int InteractionCount)> AggregateScoreAsync(EntityType entityType, Guid entityId, DateTime now, CancellationToken ct = default);
}
