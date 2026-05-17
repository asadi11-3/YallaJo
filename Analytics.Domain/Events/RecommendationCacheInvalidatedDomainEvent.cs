using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record RecommendationCacheInvalidatedDomainEvent(
    Guid UserId,
    string Reason) : DomainEventBase;
