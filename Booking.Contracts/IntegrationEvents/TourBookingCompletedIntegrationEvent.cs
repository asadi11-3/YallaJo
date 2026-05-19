using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

/// <summary>
/// Published when a Confirmed booking is marked Completed (typically after tour delivery).
/// Logical name <c>booking.tour-booking.completed.v1</c>.
/// </summary>
public sealed record TourBookingCompletedIntegrationEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    DateTime CompletedAt,
    Guid CompletedByUserId) : IntegrationEventBase;
