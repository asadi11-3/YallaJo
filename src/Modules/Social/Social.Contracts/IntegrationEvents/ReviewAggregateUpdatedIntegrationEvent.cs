using YallaJo.SharedKernel.Domain.Event;

namespace Social.Contracts.IntegrationEvents;

public sealed record ReviewAggregateUpdatedIntegrationEvent(
    string EntityType,
    Guid EntityId,
    decimal AverageRating,
    int ReviewCount,
    int BestRating,
    int WorstRating,
    DateTime UpdatedAtUtc) : IntegrationEventBase;
