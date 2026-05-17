using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record PopularityScoreUpdatedDomainEvent(
    Guid PopularityScoreId,
    string EntityType,
    Guid EntityId,
    decimal TrendingScore) : DomainEventBase;
