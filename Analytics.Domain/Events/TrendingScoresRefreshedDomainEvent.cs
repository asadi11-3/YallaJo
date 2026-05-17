using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record TrendingScoresRefreshedDomainEvent(
    int EntitiesProcessed,
    DateTime RefreshedAt) : DomainEventBase;
