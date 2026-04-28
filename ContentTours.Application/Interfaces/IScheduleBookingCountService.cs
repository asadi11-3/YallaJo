namespace ContentTours.Application.Interfaces;

/// <summary>
/// Cross-module hook: returns the number of future bookings that reference a given schedule.
/// The Booking module replaces the stub registration with a real implementation.
/// </summary>
public interface IScheduleBookingCountService
{
    Task<int> GetFutureBookingCountForScheduleAsync(Guid scheduleId, CancellationToken ct);
}
