using Tracking.Domain.Enums;
using Tracking.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Exceptions;
using YallaJo.SharedKernel.Domain.ValueObjects;

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

 

    public static LiveTrackingSession Start(
        Guid userId,
        Guid tourBookingId,
        Guid tourGuideId,
        DateTime startedAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        if (tourBookingId == Guid.Empty)
            throw new ArgumentException("Tour booking id cannot be empty.", nameof(tourBookingId));
        if (tourGuideId == Guid.Empty)
            throw new ArgumentException("Tour guide id cannot be empty.", nameof(tourGuideId));

        var session = new LiveTrackingSession
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TourBookingId = tourBookingId,
            TourGuideId = tourGuideId,
            Status = SessionStatus.Active,
            StartedAt = startedAt
        };

        session.AddDomainEvent(
            new LiveTrackingSessionStartedDomainEvent(session.Id, userId, tourBookingId, startedAt));

        return session;
    }

    public void AddCheckpoint(Guid waypointId)
    {
        if (waypointId == Guid.Empty)
            throw new ArgumentException("Waypoint id cannot be empty.", nameof(waypointId));

        if (Status != SessionStatus.Active)
            throw new BusinessRuleViolationException(
                "Checkpoints can only be added to an active session.");

        if (_tourCheckpoints.Any(c => c.WaypointId == waypointId))
            throw new BusinessRuleViolationException(
                $"A checkpoint for waypoint '{waypointId}' already exists in this session.");

        _tourCheckpoints.Add(TourCheckpoint.Create(Id, waypointId));
        MarkUpdated();
    }
    public void AddLocationSnapshot(
        decimal latitude,
        decimal longitude,
        double accuracy,
        double? speed,
        double? heading,
        double? altitude,
        DateTime capturedAt)
    {
        if (Status != SessionStatus.Active)
            throw new BusinessRuleViolationException(
                $"Location snapshots can only be added to an active session. " +
                $"Current status: {Status}.");

        // Location VO validates lat/lng ranges — throws ArgumentOutOfRangeException on violation.
        var location = new Location(latitude, longitude);

        var snapshot = LocationSnapshot.Create(
            Id, location, accuracy, speed, heading, altitude, capturedAt);

        _locationSnapshots.Add(snapshot);
        LastLocationUpdate = capturedAt;
        MarkUpdated();
    }

    public void ReachCheckpoint(Guid waypointId, DateTime reachedAt, string? notes = null)
    {
        if (Status != SessionStatus.Active)
            throw new BusinessRuleViolationException(
                $"Cannot reach a checkpoint when the session is {Status}.");

        var checkpoint = _tourCheckpoints.FirstOrDefault(c => c.WaypointId == waypointId)
            ?? throw new BusinessRuleViolationException(
                $"No checkpoint found for waypoint '{waypointId}' in this session.");

        if (checkpoint.Status != CheckpointStatus.NotReached)
            throw new BusinessRuleViolationException(
                $"Checkpoint for waypoint '{waypointId}' is already {checkpoint.Status}.");

        checkpoint.Reach(reachedAt, notes);
        AddDomainEvent(new CheckpointReachedDomainEvent(Id, waypointId, reachedAt));
        MarkUpdated();
    }

    public void SkipCheckpoint(Guid waypointId, string? notes = null)
    {
        if (Status != SessionStatus.Active)
            throw new BusinessRuleViolationException(
                $"Cannot skip a checkpoint when the session is {Status}.");

        var checkpoint = _tourCheckpoints.FirstOrDefault(c => c.WaypointId == waypointId)
            ?? throw new BusinessRuleViolationException(
                $"No checkpoint found for waypoint '{waypointId}' in this session.");

        if (checkpoint.Status == CheckpointStatus.Reached)
            throw new BusinessRuleViolationException(
                $"Cannot skip checkpoint for waypoint '{waypointId}' — it has already been reached.");

        if (checkpoint.Status == CheckpointStatus.Skipped)
            throw new BusinessRuleViolationException(
                $"Checkpoint for waypoint '{waypointId}' is already skipped.");

        checkpoint.Skip(notes);
        AddDomainEvent(new CheckpointSkippedDomainEvent(Id, waypointId));
        MarkUpdated();
    }
    public void Pause(DateTime pausedAt)
    {
        if (Status != SessionStatus.Active)
            throw new BusinessRuleViolationException(
                $"Cannot pause a session with status {Status}. Only Active sessions can be paused.");

        Status = SessionStatus.Paused;
        MarkUpdated();
        AddDomainEvent(new LiveTrackingSessionPausedDomainEvent(Id, UserId, pausedAt));
    }

    public void Resume(DateTime resumedAt)
    {
        if (Status != SessionStatus.Paused)
            throw new BusinessRuleViolationException(
                $"Cannot resume a session with status {Status}. Only Paused sessions can be resumed.");

        Status = SessionStatus.Active;
        MarkUpdated();
        AddDomainEvent(new LiveTrackingSessionResumedDomainEvent(Id, UserId, resumedAt));
    }

    public void Expire(DateTime expiredAt)
    {
        if (Status is SessionStatus.Completed or SessionStatus.Expired)
            throw new BusinessRuleViolationException(
                $"Cannot expire a session with status {Status}.");

        Status = SessionStatus.Expired;
        EndedAt = expiredAt;
        MarkUpdated();
        AddDomainEvent(new LiveTrackingSessionExpiredDomainEvent(Id, UserId, expiredAt));
    }

    public void End(string reason, DateTime endedAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("End reason cannot be empty.", nameof(reason));

        if (Status == SessionStatus.Completed)
            return; // idempotent — safe to call twice

        if (Status == SessionStatus.Expired)
            throw new BusinessRuleViolationException(
                "Cannot end an expired session. The session has already been closed.");

        Status = SessionStatus.Completed;
        EndedAt = endedAt;
        MarkUpdated();

        AddDomainEvent(new LiveTrackingSessionEndedDomainEvent(Id, UserId, endedAt, reason.Trim()));
    }
}
