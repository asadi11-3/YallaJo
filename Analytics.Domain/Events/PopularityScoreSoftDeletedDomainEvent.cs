using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record PopularityScoreSoftDeletedDomainEvent(Guid ScoreId, int EntityType, Guid EntityId, DateTime DeletedAt) : DomainEventBase;
