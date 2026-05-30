namespace Booking.Application.Queries.GetAvailabilityForTour;

public sealed record AvailabilityPage(
    IReadOnlyList<AvailabilityDateGroupDto> Items,
    string? NextCursor,
    int? TotalCount);
