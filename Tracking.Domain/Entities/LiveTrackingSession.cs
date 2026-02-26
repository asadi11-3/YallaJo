using Tracking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Tracking.Domain.Entities;

public sealed class LiveTrackingSession : AuditableEntity, IAggregateRoot
{
    private readonly List<LocationSnapshot> _locationSnapshots = [];
    private readonly List<TourCheckpoint> _tourCheckpoints = [];

    private LiveTrackingSession() { } // EF Core

    public Guid TourBookingId { get; private set; }
    public Guid TourGuideId { get; private set; }
    public SessionStatus Status { get; private set; } = SessionStatus.Active;
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public DateTime? LastLocationUpdate { get; private set; }

    public IReadOnlyCollection<LocationSnapshot> LocationSnapshots => _locationSnapshots.AsReadOnly();
    public IReadOnlyCollection<TourCheckpoint> TourCheckpoints => _tourCheckpoints.AsReadOnly();
}
