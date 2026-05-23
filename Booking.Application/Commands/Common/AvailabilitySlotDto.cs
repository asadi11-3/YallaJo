namespace Booking.Application.Commands.Common;

public sealed record AvailabilitySlotDto(
    Guid Id,
    Guid TourId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    int AvailableCount,
    string RowVersion);
