using YallaJo.SharedKernel.Domain.Event;

namespace Tracking.Domain.Events;

public sealed record CheckpointSkippedDomainEvent(
    Guid SessionId,
    Guid WaypointId) : DomainEventBase;
