using Microsoft.EntityFrameworkCore;
using Tracking.Domain.Entities;
using Tracking.Domain.Enums;
using YallaJo.SharedKernel.Domain.ValueObjects;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Tracking.Infrastructure.Persistence.Seeding;

public sealed class TrackingDbInitializer(TrackingDbContext dbContext) : IModuleDbInitializer
{
  
    private static readonly Guid GuideOne    = DevSeedIds.BookingTourGuideId;           // 5eed…000700000001
    private static readonly Guid CustomerOne = DevSeedIds.CustomerUserId;               // 5eed…000100000001

    // ── Session 1 — Active (a7a7a7a7-0000-0000-0000-0000000001xx) ─────────────
    private static readonly Guid SessionId      = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000001");
    private static readonly Guid SnapshotOneId  = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000002");
    private static readonly Guid SnapshotTwoId  = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000003");
    private static readonly Guid CheckpointOneId = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000004");
    private static readonly Guid CheckpointTwoId = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000005");

    // ── Session 2 — Completed (a7a7a7a7-0000-0000-0000-000000000010-1x) ───────
    private static readonly Guid SessionTwoId       = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000010");
    private static readonly Guid SnapshotThreeId    = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000011");
    private static readonly Guid SnapshotFourId     = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000012");
    private static readonly Guid SnapshotFiveId     = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000013");
    private static readonly Guid CheckpointThreeId  = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000014");
    private static readonly Guid CheckpointFourId   = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000015");
    private static readonly Guid CheckpointFiveId   = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000016");

    // ── Session 3 — Expired (a7a7a7a7-0000-0000-0000-000000000020-2x) ────────
    private static readonly Guid SessionThreeId  = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000020");
    private static readonly Guid SnapshotSixId   = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000021");

    // ── Waypoint logical references ────────────────────────────────────────────
    private static readonly Guid WaypointOneId   = Guid.Parse("ffffffff-1111-0000-0000-000000000001");
    private static readonly Guid WaypointTwoId   = Guid.Parse("ffffffff-1111-0000-0000-000000000002");
    private static readonly Guid WaypointThreeId = Guid.Parse("ffffffff-1111-0000-0000-000000000003");
    private static readonly Guid WaypointFourId  = Guid.Parse("ffffffff-1111-0000-0000-000000000004");
    private static readonly Guid WaypointFiveId  = Guid.Parse("ffffffff-1111-0000-0000-000000000005");

    public int Order => 140;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.LiveTrackingSessions.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.LiveTrackingSessions.AddRange(CreateSessions());
        dbContext.LocationSnapshots.AddRange(CreateSnapshots());
        dbContext.TourCheckpoints.AddRange(CreateCheckpoints());

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // ── Session builders ──────────────────────────────────────────────────────

    private static List<LiveTrackingSession> CreateSessions()
    {
        // Session 1 — Active: guide is currently tracking a live tour.
        var active = CreateEntity<LiveTrackingSession>();
        SetProperty(active, nameof(LiveTrackingSession.Id), SessionId);
        SetProperty(active, nameof(LiveTrackingSession.TourBookingId), SeedBookingIds.BookingOne);
        SetProperty(active, nameof(LiveTrackingSession.TourGuideId), GuideOne);
        SetProperty(active, nameof(LiveTrackingSession.UserId), CustomerOne);
        SetProperty(active, nameof(LiveTrackingSession.Status), SessionStatus.Active);
        SetProperty(active, nameof(LiveTrackingSession.StartedAt), DateTime.UtcNow.AddMinutes(-50));
        SetProperty(active, nameof(LiveTrackingSession.LastLocationUpdate), DateTime.UtcNow.AddMinutes(-2));

        // Session 2 — Completed: full tour run, ended normally ~yesterday.
        var completed = CreateEntity<LiveTrackingSession>();
        SetProperty(completed, nameof(LiveTrackingSession.Id), SessionTwoId);
        SetProperty(completed, nameof(LiveTrackingSession.TourBookingId), SeedBookingIds.BookingTwo);
        SetProperty(completed, nameof(LiveTrackingSession.TourGuideId), GuideOne);
        SetProperty(completed, nameof(LiveTrackingSession.UserId), CustomerOne);
        SetProperty(completed, nameof(LiveTrackingSession.Status), SessionStatus.Completed);
        SetProperty(completed, nameof(LiveTrackingSession.StartedAt), DateTime.UtcNow.AddDays(-1).AddHours(-3));
        SetProperty(completed, nameof(LiveTrackingSession.EndedAt), DateTime.UtcNow.AddDays(-1));
        SetProperty(completed, nameof(LiveTrackingSession.LastLocationUpdate), DateTime.UtcNow.AddDays(-1).AddMinutes(-5));

        // Session 3 — Expired: guide went offline mid-tour two days ago.
        var expired = CreateEntity<LiveTrackingSession>();
        SetProperty(expired, nameof(LiveTrackingSession.Id), SessionThreeId);
        SetProperty(expired, nameof(LiveTrackingSession.TourBookingId), SeedBookingIds.BookingOne);
        SetProperty(expired, nameof(LiveTrackingSession.TourGuideId), GuideOne);
        SetProperty(expired, nameof(LiveTrackingSession.UserId), CustomerOne);
        SetProperty(expired, nameof(LiveTrackingSession.Status), SessionStatus.Expired);
        SetProperty(expired, nameof(LiveTrackingSession.StartedAt), DateTime.UtcNow.AddDays(-2).AddHours(-2));
        SetProperty(expired, nameof(LiveTrackingSession.EndedAt), DateTime.UtcNow.AddDays(-2).AddMinutes(-90));
        SetProperty(expired, nameof(LiveTrackingSession.LastLocationUpdate), DateTime.UtcNow.AddDays(-2).AddMinutes(-95));

        return [active, completed, expired];
    }

    // ── Snapshot builders ─────────────────────────────────────────────────────

    private static List<LocationSnapshot> CreateSnapshots()
    {
        // ── Session 1 snapshots (Active) ──
        var s1a = CreateEntity<LocationSnapshot>();
        SetProperty(s1a, nameof(LocationSnapshot.Id), SnapshotOneId);
        SetProperty(s1a, nameof(LocationSnapshot.SessionId), SessionId);
        SetProperty(s1a, nameof(LocationSnapshot.Location), new Location(30.3270m, 35.4435m));
        SetProperty(s1a, nameof(LocationSnapshot.Accuracy), 4.2d);
        SetProperty(s1a, nameof(LocationSnapshot.Speed), 1.1d);
        SetProperty(s1a, nameof(LocationSnapshot.Heading), 92.0d);
        SetProperty(s1a, nameof(LocationSnapshot.Altitude), 905.0d);
        SetProperty(s1a, nameof(LocationSnapshot.CapturedAt), DateTime.UtcNow.AddMinutes(-10));

        var s1b = CreateEntity<LocationSnapshot>();
        SetProperty(s1b, nameof(LocationSnapshot.Id), SnapshotTwoId);
        SetProperty(s1b, nameof(LocationSnapshot.SessionId), SessionId);
        SetProperty(s1b, nameof(LocationSnapshot.Location), new Location(30.3282m, 35.4442m));
        SetProperty(s1b, nameof(LocationSnapshot.Accuracy), 3.7d);
        SetProperty(s1b, nameof(LocationSnapshot.Speed), 0.8d);
        SetProperty(s1b, nameof(LocationSnapshot.Heading), 85.0d);
        SetProperty(s1b, nameof(LocationSnapshot.Altitude), 910.0d);
        SetProperty(s1b, nameof(LocationSnapshot.CapturedAt), DateTime.UtcNow.AddMinutes(-2));

        // ── Session 2 snapshots (Completed) — 3 snapshots across ~3 hours ──
        var s2a = CreateEntity<LocationSnapshot>();
        SetProperty(s2a, nameof(LocationSnapshot.Id), SnapshotThreeId);
        SetProperty(s2a, nameof(LocationSnapshot.SessionId), SessionTwoId);
        SetProperty(s2a, nameof(LocationSnapshot.Location), new Location(30.3300m, 35.4400m));
        SetProperty(s2a, nameof(LocationSnapshot.Accuracy), 5.0d);
        SetProperty(s2a, nameof(LocationSnapshot.Speed), 1.5d);
        SetProperty(s2a, nameof(LocationSnapshot.Heading), 180.0d);
        SetProperty(s2a, nameof(LocationSnapshot.Altitude), 900.0d);
        SetProperty(s2a, nameof(LocationSnapshot.CapturedAt), DateTime.UtcNow.AddDays(-1).AddHours(-3));

        var s2b = CreateEntity<LocationSnapshot>();
        SetProperty(s2b, nameof(LocationSnapshot.Id), SnapshotFourId);
        SetProperty(s2b, nameof(LocationSnapshot.SessionId), SessionTwoId);
        SetProperty(s2b, nameof(LocationSnapshot.Location), new Location(30.3310m, 35.4410m));
        SetProperty(s2b, nameof(LocationSnapshot.Accuracy), 4.5d);
        SetProperty(s2b, nameof(LocationSnapshot.Speed), 1.2d);
        SetProperty(s2b, nameof(LocationSnapshot.Heading), 175.0d);
        SetProperty(s2b, nameof(LocationSnapshot.Altitude), 902.0d);
        SetProperty(s2b, nameof(LocationSnapshot.CapturedAt), DateTime.UtcNow.AddDays(-1).AddHours(-2));

        var s2c = CreateEntity<LocationSnapshot>();
        SetProperty(s2c, nameof(LocationSnapshot.Id), SnapshotFiveId);
        SetProperty(s2c, nameof(LocationSnapshot.SessionId), SessionTwoId);
        SetProperty(s2c, nameof(LocationSnapshot.Location), new Location(30.3320m, 35.4420m));
        SetProperty(s2c, nameof(LocationSnapshot.Accuracy), 3.8d);
        SetProperty(s2c, nameof(LocationSnapshot.Speed), 0.5d);
        SetProperty(s2c, nameof(LocationSnapshot.Heading), 170.0d);
        SetProperty(s2c, nameof(LocationSnapshot.Altitude), 908.0d);
        SetProperty(s2c, nameof(LocationSnapshot.CapturedAt), DateTime.UtcNow.AddDays(-1).AddMinutes(-5));

        // ── Session 3 snapshot (Expired) — single entry before connection loss ──
        var s3a = CreateEntity<LocationSnapshot>();
        SetProperty(s3a, nameof(LocationSnapshot.Id), SnapshotSixId);
        SetProperty(s3a, nameof(LocationSnapshot.SessionId), SessionThreeId);
        SetProperty(s3a, nameof(LocationSnapshot.Location), new Location(30.3250m, 35.4390m));
        SetProperty(s3a, nameof(LocationSnapshot.Accuracy), 8.0d);
        SetProperty(s3a, nameof(LocationSnapshot.Speed), 0.3d);
        SetProperty<double?>(s3a, nameof(LocationSnapshot.Heading), null);
        SetProperty(s3a, nameof(LocationSnapshot.Altitude), 895.0d);
        SetProperty(s3a, nameof(LocationSnapshot.CapturedAt), DateTime.UtcNow.AddDays(-2).AddMinutes(-95));

        return [s1a, s1b, s2a, s2b, s2c, s3a];
    }

    // ── Checkpoint builders ───────────────────────────────────────────────────

    private static List<TourCheckpoint> CreateCheckpoints()
    {
        // ── Session 1 checkpoints (Active) — 1 Reached, 1 NotReached ──
        var c1reached = CreateEntity<TourCheckpoint>();
        SetProperty(c1reached, nameof(TourCheckpoint.Id), CheckpointOneId);
        SetProperty(c1reached, nameof(TourCheckpoint.SessionId), SessionId);
        SetProperty(c1reached, nameof(TourCheckpoint.WaypointId), WaypointOneId);
        SetProperty(c1reached, nameof(TourCheckpoint.Status), CheckpointStatus.Reached);
        SetProperty(c1reached, nameof(TourCheckpoint.ReachedAt), DateTime.UtcNow.AddMinutes(-20));
        SetProperty(c1reached, nameof(TourCheckpoint.Notes), "Treasury waypoint reached on schedule.");

        var c1pending = CreateEntity<TourCheckpoint>();
        SetProperty(c1pending, nameof(TourCheckpoint.Id), CheckpointTwoId);
        SetProperty(c1pending, nameof(TourCheckpoint.SessionId), SessionId);
        SetProperty(c1pending, nameof(TourCheckpoint.WaypointId), WaypointTwoId);
        SetProperty(c1pending, nameof(TourCheckpoint.Status), CheckpointStatus.NotReached);
        SetProperty<DateTime?>(c1pending, nameof(TourCheckpoint.ReachedAt), null);
        SetProperty<string?>(c1pending, nameof(TourCheckpoint.Notes), null);

        // ── Session 2 checkpoints (Completed) — 1 Reached, 1 Skipped, 1 Reached ──
        var c2reached1 = CreateEntity<TourCheckpoint>();
        SetProperty(c2reached1, nameof(TourCheckpoint.Id), CheckpointThreeId);
        SetProperty(c2reached1, nameof(TourCheckpoint.SessionId), SessionTwoId);
        SetProperty(c2reached1, nameof(TourCheckpoint.WaypointId), WaypointThreeId);
        SetProperty(c2reached1, nameof(TourCheckpoint.Status), CheckpointStatus.Reached);
        SetProperty(c2reached1, nameof(TourCheckpoint.ReachedAt), DateTime.UtcNow.AddDays(-1).AddHours(-2).AddMinutes(-30));
        SetProperty(c2reached1, nameof(TourCheckpoint.Notes), "Roman ruins viewpoint — group photo taken.");

        var c2skipped = CreateEntity<TourCheckpoint>();
        SetProperty(c2skipped, nameof(TourCheckpoint.Id), CheckpointFourId);
        SetProperty(c2skipped, nameof(TourCheckpoint.SessionId), SessionTwoId);
        SetProperty(c2skipped, nameof(TourCheckpoint.WaypointId), WaypointFourId);
        SetProperty(c2skipped, nameof(TourCheckpoint.Status), CheckpointStatus.Skipped);
        SetProperty<DateTime?>(c2skipped, nameof(TourCheckpoint.ReachedAt), null);
        SetProperty(c2skipped, nameof(TourCheckpoint.Notes), "Skipped — access road closed for maintenance.");

        var c2reached2 = CreateEntity<TourCheckpoint>();
        SetProperty(c2reached2, nameof(TourCheckpoint.Id), CheckpointFiveId);
        SetProperty(c2reached2, nameof(TourCheckpoint.SessionId), SessionTwoId);
        SetProperty(c2reached2, nameof(TourCheckpoint.WaypointId), WaypointFiveId);
        SetProperty(c2reached2, nameof(TourCheckpoint.Status), CheckpointStatus.Reached);
        SetProperty(c2reached2, nameof(TourCheckpoint.ReachedAt), DateTime.UtcNow.AddDays(-1).AddMinutes(-30));
        SetProperty(c2reached2, nameof(TourCheckpoint.Notes), "Final viewpoint — tour wrap-up.");

        // Session 3 (Expired) intentionally has no checkpoints.

        return [c1reached, c1pending, c2reached1, c2skipped, c2reached2];
    }

    // ── Reflection helpers ────────────────────────────────────────────────────

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        var entity = Activator.CreateInstance(typeof(TEntity), nonPublic: true) as TEntity;
        if (entity is null)
        {
            throw new InvalidOperationException(
                $"Failed to create entity instance for {typeof(TEntity).FullName}.");
        }

        return entity;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (property is null)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' was not found on {target.GetType().FullName}.");
        }

        property.SetValue(target, value);
    }
}
