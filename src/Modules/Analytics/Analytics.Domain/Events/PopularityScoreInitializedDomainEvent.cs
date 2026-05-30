using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record PopularityScoreInitializedDomainEvent(Guid ScoreId, int EntityType, Guid EntityId, DateTime InitializedAt) : DomainEventBase;
