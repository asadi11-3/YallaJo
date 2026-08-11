using YallaJo.SharedKernel.Domain.Event;

namespace Tracking.Domain.Events;

public sealed record CheckpointReachedDomainEvent(
    Guid SessionId,
    Guid WaypointId,
    DateTime ReachedAt) : DomainEventBase;
