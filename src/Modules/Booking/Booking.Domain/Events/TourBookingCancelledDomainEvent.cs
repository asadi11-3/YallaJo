using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record TourBookingCancelledDomainEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    BookingStatus PreviousStatus,
    DateTime CancelledAt,
    CancellationSource Source,
    string? Reason,
    decimal RefundAmount,
    string Currency) : DomainEventBase;
