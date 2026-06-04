namespace Booking.Application.Queries.GetProviderBookings;

/// <summary>Cursor-paginated page of provider-scoped bookings.</summary>
public sealed record ProviderBookingsPage(
    IReadOnlyList<ProviderBookingItemDto> Items,
    string? NextCursor,
    int? TotalCount);
