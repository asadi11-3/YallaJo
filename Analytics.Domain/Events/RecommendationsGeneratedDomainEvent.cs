using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record RecommendationsGeneratedDomainEvent(
    Guid UserId,
    int RecommendationCount,
    DateTime GeneratedAt) : DomainEventBase;
