using Tracking.Domain.Entities;
using Tracking.Domain.Enums;

namespace Tracking.Application.Common;

public sealed record TrackingSessionSummaryDto(
    Guid SessionId,
    Guid TourBookingId,
    Guid TourGuideId,
    Guid UserId,
    SessionStatus Status,
    DateTime StartedAt,
    DateTime? EndedAt,
    DateTime? LastLocationUpdate);

public sealed record TrackingLocationSnapshotDto(
    Guid SnapshotId,
    Guid SessionId,
    decimal Latitude,
    decimal Longitude,
    double Accuracy,
    double? Speed,
    double? Heading,
    double? Altitude,
    DateTime CapturedAt);

public sealed record TrackingCheckpointDto(
    Guid CheckpointId,
    Guid SessionId,
    Guid WaypointId,
    CheckpointStatus Status,
    DateTime? ReachedAt,
    string? Notes);


public sealed record TrackingSessionDetailsDto(
    TrackingSessionSummaryDto Session,
    IReadOnlyList<TrackingCheckpointDto> Checkpoints,
    IReadOnlyList<TrackingLocationSnapshotDto> LocationSnapshots);

public sealed record TrackingHistoryPage(
    IReadOnlyList<TrackingSessionSummaryDto> Items,
    string? NextCursor);

internal static class TrackingMappings
{
    public static TrackingSessionSummaryDto ToSummary(this LiveTrackingSession session)
        => new(
            session.Id,
            session.TourBookingId,
            session.TourGuideId,
            session.UserId,
            session.Status,
            session.StartedAt,
            session.EndedAt,
            session.LastLocationUpdate);

    public static TrackingLocationSnapshotDto ToDto(this LocationSnapshot snapshot)
        => new(
            snapshot.Id,
            snapshot.SessionId,
            snapshot.Location.Latitude,
            snapshot.Location.Longitude,
            snapshot.Accuracy,
            snapshot.Speed,
            snapshot.Heading,
            snapshot.Altitude,
            snapshot.CapturedAt);

    public static TrackingCheckpointDto ToDto(this TourCheckpoint checkpoint)
        => new(
            checkpoint.Id,
            checkpoint.SessionId,
            checkpoint.WaypointId,
            checkpoint.Status,
            checkpoint.ReachedAt,
            checkpoint.Notes);

    public static TrackingSessionDetailsDto ToDetails(this LiveTrackingSession session)
        => new(
            session.ToSummary(),
            session.TourCheckpoints
                .OrderBy(x => x.WaypointId)
                .Select(x => x.ToDto())
                .ToArray(),
            session.LocationSnapshots
                .OrderBy(x => x.CapturedAt)
                .Select(x => x.ToDto())
                .ToArray());
}
