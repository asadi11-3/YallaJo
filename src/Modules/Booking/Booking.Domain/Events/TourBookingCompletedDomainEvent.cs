using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record TourBookingCompletedDomainEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    DateTime CompletedAt,
    Guid CompletedByUserId) : DomainEventBase;
