namespace Booking.Application.Queries.GetAvailabilityForTour;

public sealed record AvailabilitySlotListItemDto(
    Guid Id,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int AvailableCount);
