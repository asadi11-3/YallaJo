namespace Booking.Application.Queries.GetAllBookings;

/// <summary>
/// Cursor-paginated page of admin bookings.
/// </summary>
/// <param name="Items">Current page items.</param>
/// <param name="NextCursor">Opaque cursor for next page; <c>null</c> if this is the last page.</param>
/// <param name="TotalCount">Total matching rows (only populated when the caller requested <c>countTotal=true</c>).</param>
public sealed record AdminBookingsPage(
    IReadOnlyList<AdminBookingItemDto> Items,
    string? NextCursor,
    int? TotalCount);
