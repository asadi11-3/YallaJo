using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record TourBookingConfirmedDomainEvent(
    Guid BookingId,
    Guid TourId) : DomainEventBase;
