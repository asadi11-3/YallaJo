namespace Booking.Application.Queries.GetAvailabilityForTour;

public sealed record AvailabilityDateGroupDto(
    DateOnly Date,
    IReadOnlyList<AvailabilitySlotListItemDto> Slots);
