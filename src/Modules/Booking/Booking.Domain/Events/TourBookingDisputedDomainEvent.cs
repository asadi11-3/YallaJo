using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

/// <summary>
/// Raised when a user opens a dispute on a Completed booking (within the 48-hour window).
/// Consumers (Messaging) notify the provider + admin queue; Analytics may track.
/// </summary>
public sealed record TourBookingDisputedDomainEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    DateTime DisputedAt,
    string Reason) : DomainEventBase;
