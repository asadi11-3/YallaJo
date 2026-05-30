using ContentTours.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContentTours.Infrastructure.Services;

/// <summary>
/// Stub implementation of <see cref="IScheduleBookingCountService"/>.
/// Always returns 0 — the Booking module replaces this with a real implementation
/// that queries bookings WHERE ScheduleId = X AND StartsAt > UtcNow AND Status IN (Confirmed, ...).
/// </summary>
internal sealed class NoOpScheduleBookingCountService(
    ILogger<NoOpScheduleBookingCountService> logger) : IScheduleBookingCountService
{
    public Task<int> GetFutureBookingCountForScheduleAsync(Guid scheduleId, CancellationToken ct)
    {
        logger.LogDebug(
            "No booking module registered; returning 0 future bookings for scheduleId={ScheduleId}",
            scheduleId);
        return Task.FromResult(0);
    }
}
