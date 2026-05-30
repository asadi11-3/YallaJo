using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

/// <summary>
/// Published when an AwaitingPayment booking expires without successful payment. Slot capacity is restored.
/// Logical name <c>booking.tour-booking.payment-expired.v1</c>.
/// </summary>
public sealed record TourBookingPaymentExpiredIntegrationEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    DateTime CancelledAt) : IntegrationEventBase;
