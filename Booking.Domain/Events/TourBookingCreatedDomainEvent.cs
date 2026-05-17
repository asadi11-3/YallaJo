using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record TourBookingCreatedDomainEvent(
    Guid BookingId,
    Guid TourId,
    Guid UserId,
    int ParticipantCount) : DomainEventBase;
