using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

/// <summary>
/// Published when a booking is cancelled by user, provider, admin, or auto-expire.
/// Logical name <c>booking.tour-booking.cancelled.v1</c>.
/// </summary>
/// <param name="Source">String identifier: <c>User</c>, <c>Provider</c>, <c>Admin</c>, or <c>System</c>.</param>
public sealed record TourBookingCancelledIntegrationEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    DateTime CancelledAt,
    string Source,
    string? Reason,
    decimal RefundAmount,
    string Currency) : IntegrationEventBase;
