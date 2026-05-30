namespace Booking.Infrastructure.Time;

internal static class SchedulingHelpers
{
    public static DateTimeOffset NextOccurrenceUtc(TimeOnly targetUtcTime, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        var nowUtc = timeProvider.GetUtcNow();
        var todayAtTarget = new DateTimeOffset(
            nowUtc.Year,
            nowUtc.Month,
            nowUtc.Day,
            targetUtcTime.Hour,
            targetUtcTime.Minute,
            targetUtcTime.Second,
            TimeSpan.Zero);

        return todayAtTarget > nowUtc
            ? todayAtTarget
            : todayAtTarget.AddDays(1);
    }
}
