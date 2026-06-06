using Booking.Domain.Enums;

namespace Booking.Application.Commands.ResolveBookingDispute;

public sealed record ResolveBookingDisputeResult(
    Guid BookingId,
    BookingStatus Status,
    DateTime ResolvedAt,
    Guid ResolvedByAdminId,
    string ResolutionNotes);
