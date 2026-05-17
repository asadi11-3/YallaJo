using Booking.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Booking.IntegrationTests;

/// <summary>
/// PW-8 scaffold: smoke-tests that BookingDbContext boots against EF Core's SQLite
/// provider so the sprint team can hang real integration tests off it.
/// </summary>
public sealed class BookingDbContextScaffoldTests
{
    [Fact]
    public void BookingDbContext_can_construct_with_sqlite_options()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new BookingDbContext(options);

        context.TourBookings.Should().NotBeNull();
        context.AvailabilitySlots.Should().NotBeNull();
        context.OutboxMessages.Should().NotBeNull();
    }
}
