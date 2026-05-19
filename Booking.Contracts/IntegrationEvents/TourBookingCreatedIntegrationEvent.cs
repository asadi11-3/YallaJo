using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

/// <summary>
/// Published to the outbox when a booking is created in <c>AwaitingPayment</c> state.
/// Logical name in registry: <c>booking.tour-booking.created.v1</c>.
/// Consumers: Finance (pre-create Payment row), Analytics.
/// </summary>
public sealed record TourBookingCreatedIntegrationEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    decimal TotalAmount,
    string Currency,
    string Reference,
    bool IsInstantBooking,
    DateTime BookedAt) : IntegrationEventBase;
