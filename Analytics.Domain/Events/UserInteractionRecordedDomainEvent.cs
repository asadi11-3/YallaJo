using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record UserInteractionRecordedDomainEvent(
    long InteractionId,
    Guid UserId,
    InteractionType Type,
    string EntityType,
    Guid EntityId) : DomainEventBase;
