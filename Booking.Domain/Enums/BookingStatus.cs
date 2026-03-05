namespace Booking.Domain.Enums;

public enum BookingStatus : byte
{
    Pending = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Refunded = 5,
    NoShow = 6
}
