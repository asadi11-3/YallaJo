using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracking.Domain.Enums;
using Tracking.Infrastructure.Persistence;
using Tracking.Infrastructure.Persistence.Seeding;

namespace Tracking.IntegrationTests;

/// <summary>
/// Verifies that <see cref="TrackingDbInitializer"/> seeds the expected scenarios
/// into an in-memory EF Core database:
/// <list type="bullet">
///   <item>An Active session with location snapshots and checkpoints</item>
///   <item>A Completed session with checkpoints in multiple statuses (Reached, Skipped)</item>
///   <item>An Expired session</item>
/// </list>
/// Cross-module references (TourBookingId, TourGuideId, UserId, WaypointId) are
/// kept as logical Guid values — no FK constraints are verified here.
/// </summary>
public sealed class TrackingSeederRoundTripTests
{
    /// <summary>
    /// Creates an isolated in-memory <see cref="TrackingDbContext"/> and runs
    /// the seeder against it, returning the context for assertions.
    /// <para>
    /// <see cref="TrackingDbInitializer"/> only depends on <see cref="TrackingDbContext"/>;
    /// it uses reflection to bypass private constructors and calls
    /// <see cref="TrackingDbContext.SaveChangesAsync(CancellationToken)"/> directly.
    /// </para>
    /// </summary>
    private static async Task<TrackingDbContext> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<TrackingDbContext>()
            .UseInMemoryDatabase(databaseName: $"TrackingSeederTest_{Guid.NewGuid():N}")
            .Options;

        var context = new TrackingDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var seeder = new TrackingDbInitializer(context);
        await seeder.InitializeAsync(CancellationToken.None);

        return context;
    }

    // ── Session counts ────────────────────────────────────────────────────────

    [Fact]
    public async Task Seeder_creates_exactly_three_sessions()
    {
        await using var context = await SeedAsync();

        var count = await context.LiveTrackingSessions.CountAsync();

        count.Should().Be(3);
    }

    [Fact]
    public async Task Seeder_creates_one_Active_session()
    {
        await using var context = await SeedAsync();

        var activeSessions = await context.LiveTrackingSessions
            .Where(s => s.Status == SessionStatus.Active)
            .ToListAsync();

        activeSessions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Seeder_creates_one_Completed_session()
    {
        await using var context = await SeedAsync();

        var completedSessions = await context.LiveTrackingSessions
            .Where(s => s.Status == SessionStatus.Completed)
            .ToListAsync();

        completedSessions.Should().HaveCount(1);
        completedSessions[0].EndedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Seeder_creates_one_Expired_session()
    {
        await using var context = await SeedAsync();

        var expiredSessions = await context.LiveTrackingSessions
            .Where(s => s.Status == SessionStatus.Expired)
            .ToListAsync();

        expiredSessions.Should().HaveCount(1);
        expiredSessions[0].EndedAt.Should().NotBeNull();
    }

    // ── Location snapshots ────────────────────────────────────────────────────

    [Fact]
    public async Task Active_session_has_at_least_two_location_snapshots()
    {
        await using var context = await SeedAsync();

        var activeSession = await context.LiveTrackingSessions
            .Where(s => s.Status == SessionStatus.Active)
            .FirstAsync();

        var snapshotCount = await context.LocationSnapshots
            .CountAsync(snap => snap.SessionId == activeSession.Id);

        snapshotCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Completed_session_has_at_least_two_location_snapshots()
    {
        await using var context = await SeedAsync();

        var completedSession = await context.LiveTrackingSessions
            .Where(s => s.Status == SessionStatus.Completed)
            .FirstAsync();

        var snapshotCount = await context.LocationSnapshots
            .CountAsync(snap => snap.SessionId == completedSession.Id);

        snapshotCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task All_snapshots_have_valid_latitude_and_longitude()
    {
        await using var context = await SeedAsync();

        var snapshots = await context.LocationSnapshots.ToListAsync();

        snapshots.Should().NotBeEmpty();
        foreach (var snap in snapshots)
        {
            snap.Location.Latitude.Should().BeInRange(-90m, 90m);
            snap.Location.Longitude.Should().BeInRange(-180m, 180m);
        }
    }

    // ── Checkpoints ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Active_session_has_at_least_one_NotReached_checkpoint()
    {
        await using var context = await SeedAsync();

        var activeSession = await context.LiveTrackingSessions
            .Where(s => s.Status == SessionStatus.Active)
            .FirstAsync();

        var notReachedCount = await context.TourCheckpoints
            .CountAsync(c => c.SessionId == activeSession.Id
                && c.Status == CheckpointStatus.NotReached);

        notReachedCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Active_session_has_at_least_one_Reached_checkpoint()
    {
        await using var context = await SeedAsync();

        var activeSession = await context.LiveTrackingSessions
            .Where(s => s.Status == SessionStatus.Active)
            .FirstAsync();

        var reachedCount = await context.TourCheckpoints
            .CountAsync(c => c.SessionId == activeSession.Id
                && c.Status == CheckpointStatus.Reached);

        reachedCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Completed_session_has_at_least_one_Skipped_checkpoint()
    {
        await using var context = await SeedAsync();

        var completedSession = await context.LiveTrackingSessions
            .Where(s => s.Status == SessionStatus.Completed)
            .FirstAsync();

        var skippedCount = await context.TourCheckpoints
            .CountAsync(c => c.SessionId == completedSession.Id
                && c.Status == CheckpointStatus.Skipped);

        skippedCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Reached_checkpoints_have_non_null_ReachedAt()
    {
        await using var context = await SeedAsync();

        var reachedCheckpoints = await context.TourCheckpoints
            .Where(c => c.Status == CheckpointStatus.Reached)
            .ToListAsync();

        reachedCheckpoints.Should().NotBeEmpty();
        reachedCheckpoints.Should().AllSatisfy(c =>
            c.ReachedAt.Should().NotBeNull("Reached checkpoints must record when they were reached"));
    }

    [Fact]
    public async Task Skipped_checkpoints_have_null_ReachedAt()
    {
        await using var context = await SeedAsync();

        var skippedCheckpoints = await context.TourCheckpoints
            .Where(c => c.Status == CheckpointStatus.Skipped)
            .ToListAsync();

        skippedCheckpoints.Should().NotBeEmpty();
        skippedCheckpoints.Should().AllSatisfy(c =>
            c.ReachedAt.Should().BeNull("Skipped checkpoints were never physically reached"));
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Running_seeder_twice_does_not_duplicate_sessions()
    {
        var options = new DbContextOptionsBuilder<TrackingDbContext>()
            .UseInMemoryDatabase(databaseName: $"TrackingIdempotencyTest_{Guid.NewGuid():N}")
            .Options;

        await using var context = new TrackingDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var seeder = new TrackingDbInitializer(context);

        await seeder.InitializeAsync(CancellationToken.None);
        await seeder.InitializeAsync(CancellationToken.None); // second run

        var count = await context.LiveTrackingSessions.CountAsync();
        count.Should().Be(3, "seeder must be idempotent — second run must not create duplicates");
    }

    // ── Cross-module references are logical Guids (no FK assumed) ────────────

    [Fact]
    public async Task All_sessions_have_non_empty_TourBookingId_and_TourGuideId()
    {
        await using var context = await SeedAsync();

        var sessions = await context.LiveTrackingSessions.ToListAsync();

        sessions.Should().AllSatisfy(s =>
        {
            s.TourBookingId.Should().NotBeEmpty("TourBookingId is a required logical cross-module reference");
            s.TourGuideId.Should().NotBeEmpty("TourGuideId is a required logical cross-module reference");
            s.UserId.Should().NotBeEmpty("UserId is a required logical cross-module reference");
        });
    }
}
