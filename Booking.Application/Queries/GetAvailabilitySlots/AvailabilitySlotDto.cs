using Booking.Domain.Enums;

namespace Booking.Application.Queries.GetAvailabilitySlots;

public sealed record AvailabilitySlotDto(
    Guid Id,
    Guid TourGuideId,
    SlotType SlotType,
    Guid? TourId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    int BookedCount,
    int LockedCount,
    int AvailableCount,
    bool IsActive,
    decimal? PriceOverride,
    string? PriceOverrideCurrency);
