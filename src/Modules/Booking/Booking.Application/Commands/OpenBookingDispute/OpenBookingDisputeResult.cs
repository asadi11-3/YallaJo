using Booking.Domain.Enums;

namespace Booking.Application.Commands.OpenBookingDispute;

public sealed record OpenBookingDisputeResult(
    Guid BookingId,
    BookingStatus Status,
    DateTime DisputedAt,
    string Reason);
