namespace Booking.Application.Commands.CreateBulkAvailabilitySlots;

public sealed record CreateBulkAvailabilitySlotsResult(
    int CreatedCount,
    IReadOnlyList<DateOnly> SkippedDates,
    int TotalRequested);
