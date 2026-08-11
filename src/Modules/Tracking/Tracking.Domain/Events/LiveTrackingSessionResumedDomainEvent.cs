using YallaJo.SharedKernel.Domain.Event;

namespace Tracking.Domain.Events;

public sealed record LiveTrackingSessionResumedDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTime ResumedAt) : DomainEventBase;
