using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

/// <summary>
/// Published when an admin resolves a Disputed booking. Terminal state.
/// Logical name <c>booking.tour-booking.dispute-resolved.v1</c>.
/// Consumers: Messaging (notify user), Analytics (close dispute), Finance (optional refund coordination).
/// </summary>
public sealed record TourBookingDisputeResolvedIntegrationEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    Guid ResolvedByAdminId,
    DateTime ResolvedAt,
    string ResolutionNotes) : IntegrationEventBase;
