using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

/// <summary>
/// Published when a user opens a dispute on a Completed booking.
/// Logical name <c>booking.tour-booking.disputed.v1</c>.
/// Consumers: Messaging (notify provider + admin queue), Analytics (track dispute rate).
/// </summary>
public sealed record TourBookingDisputedIntegrationEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    DateTime DisputedAt,
    string Reason) : IntegrationEventBase;
