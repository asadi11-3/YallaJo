using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Booking.IntegrationTests;

/// <summary>
/// WS-1d: Proves the real (non-stub) BookingTourSnapshotReader returns the genuine
/// tour values that the enriched TourApproved event chain now persists into the
/// Booking-owned TourSnapshots table — i.e. real BasePrice, Title, IsInstantBooking
/// and MaxGroupSize, NOT the old placeholders (BasePrice=0, Title="", MaxGroupSize=int.MaxValue).
/// </summary>
public sealed class TourSnapshotReaderRoundTripTests
{
    [Fact]
    public async Task GetByIdAsync_ReturnsRealSnapshotValues_NotPlaceholders()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase($"booking-snapshot-reader-{Guid.NewGuid()}")
            .Options;

        await using var context = new BookingDbContext(options);

        var tourId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        var snapshot = TourSnapshot.Create(
            tourId: tourId,
            providerId: providerId,
            title: "Petra Full-Day Explorer",
            currency: "JOD",
            basePrice: 75m,
            isActive: true,
            isApproved: true,
            isInstantBooking: true,
            maxGroupSize: 8);

        context.TourSnapshots.Add(snapshot);
        await context.SaveChangesAsync();

        var reader = CreateReader(context);

        // Act
        var result = await reader.GetByIdAsync(tourId);

        // Assert — real values flowed through, not the old placeholders
        result.Should().NotBeNull();
        result!.TourId.Should().Be(tourId);
        result.ProviderId.Should().Be(providerId);
        result.Title.Should().Be("Petra Full-Day Explorer");
        result.Currency.Should().Be("JOD");
        result.BasePrice.Should().Be(75m);
        result.IsInstantBooking.Should().BeTrue();
        result.MaxGroupSize.Should().Be(8);

        // Guard against regression to the stub/placeholder behaviour.
        result.BasePrice.Should().NotBe(0m);
        result.Title.Should().NotBeNullOrEmpty();
        result.MaxGroupSize.Should().NotBe(int.MaxValue);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenSnapshotMissing()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase($"booking-snapshot-reader-{Guid.NewGuid()}")
            .Options;

        await using var context = new BookingDbContext(options);

        var reader = CreateReader(context);

        var result = await reader.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    private static IBookingTourSnapshotReader CreateReader(BookingDbContext context)
    {
        // BookingTourSnapshotReader is internal sealed in Booking.Infrastructure.Services.
        var readerType = typeof(Booking.Infrastructure.Persistence.BookingDbContext).Assembly
            .GetType("Booking.Infrastructure.Services.BookingTourSnapshotReader");

        readerType.Should().NotBeNull("the real BookingTourSnapshotReader type must exist");

        var instance = Activator.CreateInstance(readerType!, context);
        return (IBookingTourSnapshotReader)instance!;
    }
}
