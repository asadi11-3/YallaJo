using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record PopularityScoreRecalculatedDomainEvent(Guid ScoreId, int EntityType, Guid EntityId, decimal OldScore, decimal NewScore, int InteractionCount, DateTime RecalculatedAt) : DomainEventBase;
