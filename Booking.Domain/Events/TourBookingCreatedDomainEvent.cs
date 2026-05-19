using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

/// <summary>
/// Raised in-process when a booking is created (still in <c>AwaitingPayment</c>).
/// Translated to <c>TourBookingCreatedIntegrationEvent</c> for cross-module dispatch via outbox.
/// </summary>
public sealed record TourBookingCreatedDomainEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    decimal TotalAmount,
    string Currency,
    string Reference,
    bool IsInstantBooking) : DomainEventBase;
