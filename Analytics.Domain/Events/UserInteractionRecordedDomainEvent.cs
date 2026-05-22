using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record UserInteractionRecordedDomainEvent(Guid? UserId, string? SessionId, int EntityType, Guid EntityId, int InteractionType, DateTime OccurredAt) : DomainEventBase;
