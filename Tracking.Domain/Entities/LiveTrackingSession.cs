using Tracking.Domain.Enums;
using Tracking.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Tracking.Domain.Entities;

public sealed class LiveTrackingSession : AuditableEntity, IAggregateRoot
{
    private readonly List<LocationSnapshot> _locationSnapshots = [];
    private readonly List<TourCheckpoint> _tourCheckpoints = [];

    private LiveTrackingSession() { } // EF Core

    public Guid TourBookingId { get; private set; }
    public Guid TourGuideId { get; private set; }
    public Guid UserId { get; private set; }
    public SessionStatus Status { get; private set; } = SessionStatus.Active;
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public DateTime? LastLocationUpdate { get; private set; }

    public IReadOnlyCollection<LocationSnapshot> LocationSnapshots => _locationSnapshots.AsReadOnly();
    public IReadOnlyCollection<TourCheckpoint> TourCheckpoints => _tourCheckpoints.AsReadOnly();

    public static LiveTrackingSession Start(Guid userId, Guid tourBookingId, Guid tourGuideId, DateTime startedAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        if (tourBookingId == Guid.Empty) throw new ArgumentException("Tour booking id cannot be empty.", nameof(tourBookingId));
        if (tourGuideId == Guid.Empty) throw new ArgumentException("Tour guide id cannot be empty.", nameof(tourGuideId));

        var session = new LiveTrackingSession
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TourBookingId = tourBookingId,
            TourGuideId = tourGuideId,
            Status = SessionStatus.Active,
            StartedAt = startedAt
        };

        session.AddDomainEvent(new LiveTrackingSessionStartedDomainEvent(session.Id, userId, tourBookingId, startedAt));
        return session;
    }

    public void End(string reason, DateTime endedAt)
    {
        if (Status == SessionStatus.Completed) return;

        Status = SessionStatus.Completed;
        EndedAt = endedAt;
        MarkUpdated();

        AddDomainEvent(new LiveTrackingSessionEndedDomainEvent(Id, UserId, endedAt, reason.Trim()));
    }
}
