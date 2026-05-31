using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record TourBookingConfirmedDomainEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    DateTime ConfirmedAt,
    ConfirmationSource Source) : DomainEventBase;
