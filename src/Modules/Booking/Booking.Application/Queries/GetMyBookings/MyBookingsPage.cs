namespace Booking.Application.Queries.GetMyBookings;

/// <summary>
/// Cursor-paginated response for booking list endpoints.
/// </summary>
public sealed record MyBookingsPage(
    IReadOnlyList<MyBookingItemDto> Items,
    string? NextCursor,
    int? TotalCount);
