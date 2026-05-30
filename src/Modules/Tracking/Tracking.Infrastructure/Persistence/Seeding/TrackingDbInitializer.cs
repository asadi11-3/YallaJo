using Microsoft.EntityFrameworkCore;
using Tracking.Domain.Entities;
using Tracking.Domain.Enums;
using YallaJo.SharedKernel.Domain.ValueObjects;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Tracking.Infrastructure.Persistence.Seeding;

public sealed class TrackingDbInitializer(TrackingDbContext dbContext) : IModuleDbInitializer
{
    private static readonly Guid GuideOne = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SessionId = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000001");
    private static readonly Guid SnapshotOneId = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000002");
    private static readonly Guid SnapshotTwoId = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000003");
    private static readonly Guid CheckpointOneId = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000004");
    private static readonly Guid CheckpointTwoId = Guid.Parse("a7a7a7a7-0000-0000-0000-000000000005");
    private static readonly Guid WaypointOneId = Guid.Parse("ffffffff-1111-0000-0000-000000000001");
    private static readonly Guid WaypointTwoId = Guid.Parse("ffffffff-1111-0000-0000-000000000002");

    public int Order => 140;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.LiveTrackingSessions.AnyAsync(cancellationToken))
        {
            return;
        }

        var sessions = CreateSessions();
        var snapshots = CreateSnapshots();
        var checkpoints = CreateCheckpoints();

        dbContext.LiveTrackingSessions.AddRange(sessions);
        dbContext.LocationSnapshots.AddRange(snapshots);
        dbContext.TourCheckpoints.AddRange(checkpoints);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<LiveTrackingSession> CreateSessions()
    {
        var session = CreateEntity<LiveTrackingSession>();
        SetProperty(session, nameof(LiveTrackingSession.Id), SessionId);
        SetProperty(session, nameof(LiveTrackingSession.TourBookingId), SeedBookingIds.BookingOne);
        SetProperty(session, nameof(LiveTrackingSession.TourGuideId), GuideOne);
        SetProperty(session, nameof(LiveTrackingSession.Status), SessionStatus.Active);
        SetProperty(session, nameof(LiveTrackingSession.StartedAt), DateTime.UtcNow.AddMinutes(-50));
        SetProperty(session, nameof(LiveTrackingSession.LastLocationUpdate), DateTime.UtcNow.AddMinutes(-2));
        return [session];
    }

    private static List<LocationSnapshot> CreateSnapshots()
    {
        var first = CreateEntity<LocationSnapshot>();
        SetProperty(first, nameof(LocationSnapshot.Id), SnapshotOneId);
        SetProperty(first, nameof(LocationSnapshot.SessionId), SessionId);
        SetProperty(first, nameof(LocationSnapshot.Location), new Location(30.3270m, 35.4435m));
        SetProperty(first, nameof(LocationSnapshot.Accuracy), 4.2d);
        SetProperty(first, nameof(LocationSnapshot.Speed), 1.1d);
        SetProperty(first, nameof(LocationSnapshot.Heading), 92.0d);
        SetProperty(first, nameof(LocationSnapshot.Altitude), 905.0d);
        SetProperty(first, nameof(LocationSnapshot.CapturedAt), DateTime.UtcNow.AddMinutes(-10));

        var second = CreateEntity<LocationSnapshot>();
        SetProperty(second, nameof(LocationSnapshot.Id), SnapshotTwoId);
        SetProperty(second, nameof(LocationSnapshot.SessionId), SessionId);
        SetProperty(second, nameof(LocationSnapshot.Location), new Location(30.3282m, 35.4442m));
        SetProperty(second, nameof(LocationSnapshot.Accuracy), 3.7d);
        SetProperty(second, nameof(LocationSnapshot.Speed), 0.8d);
        SetProperty(second, nameof(LocationSnapshot.Heading), 85.0d);
        SetProperty(second, nameof(LocationSnapshot.Altitude), 910.0d);
        SetProperty(second, nameof(LocationSnapshot.CapturedAt), DateTime.UtcNow.AddMinutes(-2));

        return [first, second];
    }

    private static List<TourCheckpoint> CreateCheckpoints()
    {
        var reached = CreateEntity<TourCheckpoint>();
        SetProperty(reached, nameof(TourCheckpoint.Id), CheckpointOneId);
        SetProperty(reached, nameof(TourCheckpoint.SessionId), SessionId);
        SetProperty(reached, nameof(TourCheckpoint.WaypointId), WaypointOneId);
        SetProperty(reached, nameof(TourCheckpoint.Status), CheckpointStatus.Reached);
        SetProperty(reached, nameof(TourCheckpoint.ReachedAt), DateTime.UtcNow.AddMinutes(-20));
        SetProperty(reached, nameof(TourCheckpoint.Notes), "Treasury waypoint reached on schedule.");

        var pending = CreateEntity<TourCheckpoint>();
        SetProperty(pending, nameof(TourCheckpoint.Id), CheckpointTwoId);
        SetProperty(pending, nameof(TourCheckpoint.SessionId), SessionId);
        SetProperty(pending, nameof(TourCheckpoint.WaypointId), WaypointTwoId);
        SetProperty(pending, nameof(TourCheckpoint.Status), CheckpointStatus.NotReached);
        SetProperty<DateTime?>(pending, nameof(TourCheckpoint.ReachedAt), null);
        SetProperty(pending, nameof(TourCheckpoint.Notes), "Monastery waypoint expected in next segment.");

        return [reached, pending];
    }

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        var entity = Activator.CreateInstance(typeof(TEntity), nonPublic: true) as TEntity;
        if (entity is null)
        {
            throw new InvalidOperationException($"Failed to create entity instance for {typeof(TEntity).FullName}.");
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
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");
        }

        property.SetValue(target, value);
    }
}
