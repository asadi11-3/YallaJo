namespace Booking.Application.Queries.GetManageAvailabilityForTour;

/// <summary>
/// Owner-facing availability slot projection for provider slot management.
/// Includes RowVersion (for edit/delete concurrency) and IsActive, unlike the
/// public availability list. Exposes only slot fields — no unrelated booking data.
/// </summary>
public sealed record ManageAvailabilitySlotDto(
    Guid Id,
    Guid TourId,
    Guid TourGuideId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    int AvailableCount,
    bool IsActive,
    string RowVersion);
