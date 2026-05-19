using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record TourBookingConfirmedDomainEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    DateTime ConfirmedAt,
    ConfirmationSource Source) : DomainEventBase;
