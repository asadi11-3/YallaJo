using Booking.Domain.Enums;

namespace Booking.Application.Commands.RejectTourBooking;

public sealed record RejectTourBookingResult(
    Guid BookingId,
    BookingStatus Status,
    DateTime RejectedAt,
    string Reason,
    decimal RefundAmount,
    string Currency);
