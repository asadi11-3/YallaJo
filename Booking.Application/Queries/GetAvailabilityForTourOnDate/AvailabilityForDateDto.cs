using Booking.Application.Queries.GetAvailabilityForTour;

namespace Booking.Application.Queries.GetAvailabilityForTourOnDate;

public sealed record AvailabilityForDateDto(
    IReadOnlyList<AvailabilitySlotListItemDto> Slots);
