namespace ContentTours.Application.Interfaces;

public interface IScheduleBookingCountService
{
    Task<int> GetFutureBookingCountForScheduleAsync(Guid scheduleId, CancellationToken ct);
}
