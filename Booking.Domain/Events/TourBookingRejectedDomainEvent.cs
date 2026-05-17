using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record TourBookingRejectedDomainEvent(
    Guid BookingId,
    Guid TourId,
    string Reason) : DomainEventBase;
