using Tracking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Tracking.Domain.Entities;

public sealed class TourCheckpoint : AuditableEntity
{
    private TourCheckpoint() { } // EF Core

    public Guid SessionId { get; private set; }
    public Guid WaypointId { get; private set; }
    public CheckpointStatus Status { get; private set; } = CheckpointStatus.NotReached;
    public DateTime? ReachedAt { get; private set; }
    public string? Notes { get; private set; }

    public LiveTrackingSession LiveTrackingSession { get; private set; } = null!;

    internal static TourCheckpoint Create(Guid sessionId, Guid waypointId)
        => new()
        {
            Id = Guid.CreateVersion7(),
            SessionId = sessionId,
            WaypointId = waypointId,
            Status = CheckpointStatus.NotReached
        };

    internal void Reach(DateTime reachedAt, string? notes)
    {
        Status = CheckpointStatus.Reached;
        ReachedAt = reachedAt;
        Notes = notes?.Trim();
        MarkUpdated();
    }

    internal void Skip(string? notes)
    {
        Status = CheckpointStatus.Skipped;
        Notes = notes?.Trim();
        MarkUpdated();
    }
}
