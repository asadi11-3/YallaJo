using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

/// <summary>
/// Published when a provider rejects a PendingConfirmation booking. Finance auto-issues a 100% refund.
/// Logical name <c>booking.tour-booking.rejected.v1</c>.
/// </summary>
public sealed record TourBookingRejectedIntegrationEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    DateTime RejectedAt,
    string Reason,
    decimal RefundAmount,
    string Currency) : IntegrationEventBase;
