using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Booking.Infrastructure.BackgroundServices.Options;
using Booking.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Booking.Tests.Unit.Services;

public sealed class SnapshotBookingCommissionLookupTests
{
    private static IOptions<BookingCommissionDefaultsOptions> Defaults(
        string tier = "Free", string currency = "JOD", decimal fallback = 0.10m)
        => Options.Create(new BookingCommissionDefaultsOptions
        {
            Tier = tier,
            Currency = currency,
            FallbackRate = fallback,
        });

    [Fact]
    public async Task Returns_matching_snapshot_rate_as_fraction()
    {
        var repo = Substitute.For<ICommissionSnapshotRepository>();
        // Finance percent (e.g. 12.5%) must be returned as the fraction 0.125.
        var snapshot = CommissionSnapshot.Create(
            Guid.NewGuid(), "Free", "JOD", 12.5m, DateTime.UtcNow);
        repo.GetActiveByTierAsync("Free", "JOD", Arg.Any<CancellationToken>())
            .Returns(snapshot);

        var sut = new SnapshotBookingCommissionLookup(
            repo, Defaults(), NullLogger<SnapshotBookingCommissionLookup>.Instance);

        var result = await sut.GetForTourAsync(Guid.NewGuid(), CancellationToken.None);

        result.Rate.Should().Be(0.125m);
    }

    [Fact]
    public async Task No_snapshot_falls_back_to_configured_rate()
    {
        var repo = Substitute.For<ICommissionSnapshotRepository>();
        repo.GetActiveByTierAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CommissionSnapshot?)null);

        var sut = new SnapshotBookingCommissionLookup(
            repo, Defaults(fallback: 0.07m), NullLogger<SnapshotBookingCommissionLookup>.Instance);

        var result = await sut.GetForTourAsync(Guid.NewGuid(), CancellationToken.None);

        result.Rate.Should().Be(0.07m);
    }

    [Fact]
    public async Task No_snapshot_default_fallback_is_zero_point_one()
    {
        // Behavior continuity with the previous StubBookingCommissionLookup.
        var repo = Substitute.For<ICommissionSnapshotRepository>();
        repo.GetActiveByTierAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CommissionSnapshot?)null);

        var sut = new SnapshotBookingCommissionLookup(
            repo, Defaults(), NullLogger<SnapshotBookingCommissionLookup>.Instance);

        var result = await sut.GetForTourAsync(Guid.NewGuid(), CancellationToken.None);

        result.Rate.Should().Be(0.10m);
    }

    [Fact]
    public async Task Honors_configured_Tier_and_Currency_when_querying_repo()
    {
        var repo = Substitute.For<ICommissionSnapshotRepository>();
        var snapshot = CommissionSnapshot.Create(
            Guid.NewGuid(), "Premium", "USD", 8m, DateTime.UtcNow);
        repo.GetActiveByTierAsync("Premium", "USD", Arg.Any<CancellationToken>())
            .Returns(snapshot);

        var sut = new SnapshotBookingCommissionLookup(
            repo, Defaults("Premium", "USD"), NullLogger<SnapshotBookingCommissionLookup>.Instance);

        var result = await sut.GetForTourAsync(Guid.NewGuid(), CancellationToken.None);

        result.Rate.Should().Be(0.08m);
        await repo.Received(1).GetActiveByTierAsync("Premium", "USD", Arg.Any<CancellationToken>());
    }
}
