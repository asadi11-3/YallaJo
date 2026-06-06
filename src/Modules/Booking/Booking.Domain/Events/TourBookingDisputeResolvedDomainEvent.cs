using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

/// <summary>
/// Raised when an admin resolves a Disputed booking. Terminal state for the dispute lifecycle.
/// </summary>
public sealed record TourBookingDisputeResolvedDomainEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    Guid ResolvedByAdminId,
    DateTime ResolvedAt,
    string ResolutionNotes) : DomainEventBase;
