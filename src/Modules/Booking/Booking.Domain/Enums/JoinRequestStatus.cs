namespace Booking.Domain.Enums;

public enum JoinRequestStatus : byte
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
    Expired = 4
}
