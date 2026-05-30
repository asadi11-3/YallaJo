using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record TourBookingPaymentExpiredDomainEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    DateTime CancelledAt) : DomainEventBase;
