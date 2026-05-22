using YallaJo.SharedKernel.Domain.Event;

namespace Tracking.Domain.Events;

public sealed record LiveTrackingSessionStartedDomainEvent(
    Guid SessionId,
    Guid UserId,
    Guid TourBookingId,
    DateTime StartedAt) : DomainEventBase;
