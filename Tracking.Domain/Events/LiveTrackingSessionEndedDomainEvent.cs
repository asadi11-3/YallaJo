using YallaJo.SharedKernel.Domain.Event;

namespace Tracking.Domain.Events;

public sealed record LiveTrackingSessionEndedDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTime EndedAt,
    string Reason) : DomainEventBase;
