namespace ContentTours.Contracts.Tours;

/// <summary>
/// Read-only cross-module access to guide schedules, used by the Booking module
/// to generate AvailabilitySlots for the rolling 60-day window.
/// </summary>
public interface IGuideScheduleReader
{
    /// <summary>
    /// Returns all active guide schedules with their associated TourGuideId and TourId.
    /// </summary>
    Task<IReadOnlyList<GuideScheduleSummary>> GetActiveSchedulesAsync(CancellationToken ct = default);
}

/// <summary>Lightweight projection of a GuideSchedule for slot generation.</summary>
public sealed record GuideScheduleSummary(
    Guid ScheduleId,
    Guid GuideTourOfferingId,
    Guid TourGuideId,
    Guid TourId,
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime);
