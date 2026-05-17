using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record JoinRequestCreatedDomainEvent(
    Guid JoinRequestId,
    Guid TourBookingId,
    Guid UserId) : DomainEventBase;
