using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracking.Infrastructure.Persistence;

namespace Tracking.IntegrationTests;

/// <summary>
/// Smoke-tests that <see cref="TrackingDbContext"/> boots correctly via EF Core's
/// SQLite in-memory provider. Provides a scaffold for attaching further
/// integration tests as the module grows.
/// </summary>
public sealed class TrackingDbContextScaffoldTests
{
    [Fact]
    public void TrackingDbContext_can_construct_with_sqlite_options()
    {
        var options = new DbContextOptionsBuilder<TrackingDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new TrackingDbContext(options);

        context.LiveTrackingSessions.Should().NotBeNull();
        context.LocationSnapshots.Should().NotBeNull();
        context.TourCheckpoints.Should().NotBeNull();
        context.OutboxMessages.Should().NotBeNull();
    }

    [Fact]
    public void TrackingDbContext_has_default_schema_tracking()
    {
        var options = new DbContextOptionsBuilder<TrackingDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new TrackingDbContext(options);

        var model = context.Model;
        model.Should().NotBeNull();

        // All entity types should be configured (configuration assembly scan runs)
        var entityTypes = model.GetEntityTypes().Select(e => e.ClrType.Name).ToList();
        entityTypes.Should().Contain("LiveTrackingSession");
        entityTypes.Should().Contain("LocationSnapshot");
        entityTypes.Should().Contain("TourCheckpoint");
        entityTypes.Should().Contain("OutboxMessage");
    }

    [Fact]
    public async Task TrackingDbContext_can_create_schema_and_insert_session()
    {
        var options = new DbContextOptionsBuilder<TrackingDbContext>()
            .UseInMemoryDatabase(databaseName: $"TrackingTest_{Guid.NewGuid():N}")
            .Options;

        await using var context = new TrackingDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var session = Tracking.Domain.Entities.LiveTrackingSession.Start(
            userId: Guid.NewGuid(),
            tourBookingId: Guid.NewGuid(),
            tourGuideId: Guid.NewGuid(),
            startedAt: DateTime.UtcNow);

        context.LiveTrackingSessions.Add(session);
        await context.SaveChangesAsync();

        var saved = await context.LiveTrackingSessions.FindAsync(session.Id);
        saved.Should().NotBeNull();
        saved!.Status.Should().Be(Tracking.Domain.Enums.SessionStatus.Active);
    }
}
