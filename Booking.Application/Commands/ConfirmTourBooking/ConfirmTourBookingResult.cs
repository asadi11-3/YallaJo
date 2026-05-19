using Booking.Domain.Enums;

namespace Booking.Application.Commands.ConfirmTourBooking;

public sealed record ConfirmTourBookingResult(
    Guid BookingId,
    BookingStatus Status,
    DateTime ConfirmedAt,
    ConfirmationSource Source);
