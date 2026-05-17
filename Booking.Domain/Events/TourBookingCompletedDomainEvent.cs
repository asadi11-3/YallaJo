using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record TourBookingCompletedDomainEvent(
    Guid BookingId,
    Guid TourId) : DomainEventBase;
