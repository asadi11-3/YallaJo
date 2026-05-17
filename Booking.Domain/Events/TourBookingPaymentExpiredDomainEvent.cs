using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record TourBookingPaymentExpiredDomainEvent(
    Guid BookingId,
    Guid TourId) : DomainEventBase;
