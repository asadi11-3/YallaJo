using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record PopularityScoreStaleFlaggedDomainEvent(Guid ScoreId, int EntityType, Guid EntityId, DateTime FlaggedAt) : DomainEventBase;
