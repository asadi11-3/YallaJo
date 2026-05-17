using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record TourBookingCancelledDomainEvent(
    Guid BookingId,
    Guid TourId,
    string Reason) : DomainEventBase;
