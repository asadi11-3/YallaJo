using YallaJo.SharedKernel.Domain.Event;

namespace Tracking.Domain.Events;

public sealed record LiveTrackingSessionPausedDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTime PausedAt) : DomainEventBase;
