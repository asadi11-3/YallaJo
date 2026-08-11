using YallaJo.SharedKernel.Domain.Event;

namespace Tracking.Domain.Events;

public sealed record LiveTrackingSessionExpiredDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTime ExpiredAt) : DomainEventBase;
