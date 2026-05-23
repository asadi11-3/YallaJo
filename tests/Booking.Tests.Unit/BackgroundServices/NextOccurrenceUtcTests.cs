using System.Reflection;
using FluentAssertions;

namespace Booking.Tests.Unit.BackgroundServices;

public sealed class NextOccurrenceUtcTests
{
    private static readonly MethodInfo NextOccurrenceMethod = typeof(Booking.Infrastructure.DependencyInjection)
        .Assembly
        .GetType("Booking.Infrastructure.Time.SchedulingHelpers", throwOnError: true)!
        .GetMethod("NextOccurrenceUtc", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("NextOccurrenceUtc helper not found.");

    [Fact]
    public void Returns_today_when_target_time_is_later_today()
    {
        var nowUtc = DateTimeOffset.Parse("2026-07-15T06:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var time = new TimeOnly(15, 30);
        var tp = new FakeTimeProvider(nowUtc);

        var next = (DateTimeOffset)NextOccurrenceMethod.Invoke(null, [time, tp])!;

        next.Should().Be(new DateTimeOffset(2026, 7, 15, 15, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Returns_tomorrow_when_target_time_already_passed_today()
    {
        var nowUtc = DateTimeOffset.Parse("2026-07-15T23:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var time = new TimeOnly(1, 0);
        var tp = new FakeTimeProvider(nowUtc);

        var next = (DateTimeOffset)NextOccurrenceMethod.Invoke(null, [time, tp])!;

        next.Should().Be(new DateTimeOffset(2026, 7, 16, 1, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Returns_tomorrow_when_target_time_equals_now()
    {
        // Boundary: today's instance is NOT strictly in the future, so we move to tomorrow.
        var nowUtc = DateTimeOffset.Parse("2026-07-15T01:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var time = new TimeOnly(1, 0);
        var tp = new FakeTimeProvider(nowUtc);

        var next = (DateTimeOffset)NextOccurrenceMethod.Invoke(null, [time, tp])!;

        next.Should().Be(new DateTimeOffset(2026, 7, 16, 1, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Uses_time_provider_not_machine_clock()
    {
        // Fake "now" set far in the future; the result must follow the fake.
        var nowUtc = new DateTimeOffset(2099, 1, 1, 0, 30, 0, TimeSpan.Zero);
        var time = new TimeOnly(0, 0);
        var tp = new FakeTimeProvider(nowUtc);

        var next = (DateTimeOffset)NextOccurrenceMethod.Invoke(null, [time, tp])!;

        next.Should().Be(new DateTimeOffset(2099, 1, 2, 0, 0, 0, TimeSpan.Zero));
    }
}
