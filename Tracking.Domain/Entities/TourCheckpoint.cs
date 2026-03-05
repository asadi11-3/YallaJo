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
}
