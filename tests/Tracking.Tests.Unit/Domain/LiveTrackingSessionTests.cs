using FluentAssertions;
using Tracking.Domain.Entities;
using Tracking.Domain.Enums;
using Tracking.Domain.Events;
using YallaJo.SharedKernel.Domain.Exceptions;
using YallaJo.Tests.Shared;

namespace Tracking.Tests.Unit.Domain;

/// <summary>
/// Unit tests for the <see cref="LiveTrackingSession"/> aggregate root.
/// Covers the Start factory, AddLocationSnapshot, Pause, Resume, Expire, End,
/// ReachCheckpoint, SkipCheckpoint, and all associated invariants.
/// </summary>
public sealed class LiveTrackingSessionTests : DomainTestBase
{
    private static readonly Guid UserId        = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TourBookingId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TourGuideId   = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid WaypointA     = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid WaypointB     = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly DateTime Now        = DateTime.UtcNow;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LiveTrackingSession CreateActiveSession()
        => LiveTrackingSession.Start(UserId, TourBookingId, TourGuideId, Now.AddMinutes(-10));

    private static LiveTrackingSession CreateSessionWithCheckpoints()
    {
        var session = CreateActiveSession();
        session.AddCheckpoint(WaypointA);
        session.AddCheckpoint(WaypointB);
        session.ClearDomainEvents();
        return session;
    }

    // ── Start factory ─────────────────────────────────────────────────────────

    [Fact]
    public void Start_with_valid_inputs_creates_Active_session_and_raises_StartedEvent()
    {
        var session = LiveTrackingSession.Start(UserId, TourBookingId, TourGuideId, Now);

        session.Status.Should().Be(SessionStatus.Active);
        session.UserId.Should().Be(UserId);
        session.TourBookingId.Should().Be(TourBookingId);
        session.TourGuideId.Should().Be(TourGuideId);
        session.StartedAt.Should().Be(Now);
        session.EndedAt.Should().BeNull();

        var evt = session.ShouldContainDomainEvent<LiveTrackingSessionStartedDomainEvent>();
        evt.SessionId.Should().Be(session.Id);
        evt.UserId.Should().Be(UserId);
        evt.TourBookingId.Should().Be(TourBookingId);
    }

    [Theory]
    [InlineData(true, false, false)]  // empty userId
    [InlineData(false, true, false)]  // empty tourBookingId
    [InlineData(false, false, true)]  // empty tourGuideId
    public void Start_with_empty_guid_throws_ArgumentException(
        bool emptyUser, bool emptyBooking, bool emptyGuide)
    {
        var userId        = emptyUser    ? Guid.Empty : UserId;
        var tourBookingId = emptyBooking ? Guid.Empty : TourBookingId;
        var tourGuideId   = emptyGuide   ? Guid.Empty : TourGuideId;

        var act = () => LiveTrackingSession.Start(userId, tourBookingId, tourGuideId, Now);

        act.Should().Throw<ArgumentException>();
    }

    // ── AddLocationSnapshot ───────────────────────────────────────────────────

    [Fact]
    public void AddLocationSnapshot_on_Active_session_adds_snapshot_and_updates_LastLocationUpdate()
    {
        var session = CreateActiveSession();
        var captured = Now;

        session.AddLocationSnapshot(30.33m, 35.44m, 4.5, 1.2, 90.0, 900.0, captured);

        session.LocationSnapshots.Should().HaveCount(1);
        session.LastLocationUpdate.Should().Be(captured);
        var snap = session.LocationSnapshots.First();
        snap.Location.Latitude.Should().Be(30.33m);
        snap.Location.Longitude.Should().Be(35.44m);
        snap.Accuracy.Should().Be(4.5);
    }

    [Fact]
    public void AddLocationSnapshot_with_invalid_latitude_throws_ArgumentOutOfRangeException()
    {
        var session = CreateActiveSession();

        var act = () => session.AddLocationSnapshot(91m, 35.44m, 4.5, null, null, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddLocationSnapshot_with_invalid_longitude_throws_ArgumentOutOfRangeException()
    {
        var session = CreateActiveSession();

        var act = () => session.AddLocationSnapshot(30.33m, 181m, 4.5, null, null, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddLocationSnapshot_on_Completed_session_throws_BusinessRuleViolationException()
    {
        var session = CreateActiveSession();
        session.End("Tour ended.", Now);

        var act = () => session.AddLocationSnapshot(30.33m, 35.44m, 4.5, null, null, null, Now);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void AddLocationSnapshot_on_Paused_session_throws_BusinessRuleViolationException()
    {
        var session = CreateActiveSession();
        session.Pause(Now);

        var act = () => session.AddLocationSnapshot(30.33m, 35.44m, 4.5, null, null, null, Now);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ── Pause / Resume ────────────────────────────────────────────────────────

    [Fact]
    public void Pause_transitions_Active_to_Paused_and_raises_PausedEvent()
    {
        var session = CreateActiveSession();

        session.Pause(Now);

        session.Status.Should().Be(SessionStatus.Paused);
        session.ShouldContainDomainEvent<LiveTrackingSessionPausedDomainEvent>()
            .SessionId.Should().Be(session.Id);
    }

    [Fact]
    public void Pause_on_non_Active_session_throws_BusinessRuleViolationException()
    {
        var session = CreateActiveSession();
        session.End("Done.", Now);

        var act = () => session.Pause(Now);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Resume_transitions_Paused_to_Active_and_raises_ResumedEvent()
    {
        var session = CreateActiveSession();
        session.Pause(Now);
        session.ClearDomainEvents();

        session.Resume(Now.AddMinutes(5));

        session.Status.Should().Be(SessionStatus.Active);
        session.ShouldContainDomainEvent<LiveTrackingSessionResumedDomainEvent>()
            .SessionId.Should().Be(session.Id);
    }

    [Fact]
    public void Resume_on_non_Paused_session_throws_BusinessRuleViolationException()
    {
        var session = CreateActiveSession(); // Active, not Paused

        var act = () => session.Resume(Now);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ── End ───────────────────────────────────────────────────────────────────

    [Fact]
    public void End_transitions_Active_to_Completed_and_raises_EndedEvent()
    {
        var session = CreateActiveSession();

        session.End("Tour completed normally.", Now);

        session.Status.Should().Be(SessionStatus.Completed);
        session.EndedAt.Should().Be(Now);
        var evt = session.ShouldContainDomainEvent<LiveTrackingSessionEndedDomainEvent>();
        evt.Reason.Should().Be("Tour completed normally.");
    }

    [Fact]
    public void End_on_already_Completed_session_is_idempotent_no_event()
    {
        var session = CreateActiveSession();
        session.End("First end.", Now);
        session.ClearDomainEvents();

        // Should not throw — idempotent
        session.End("Second call.", Now.AddSeconds(1));

        session.Status.Should().Be(SessionStatus.Completed);
        session.DomainEvents.Should().BeEmpty(); // no second event raised
    }

    [Fact]
    public void End_on_Expired_session_throws_BusinessRuleViolationException()
    {
        var session = CreateActiveSession();
        session.Expire(Now);

        var act = () => session.End("Too late.", Now.AddMinutes(1));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void End_with_empty_reason_throws_ArgumentException()
    {
        var session = CreateActiveSession();

        var act = () => session.End("   ", Now);

        act.Should().Throw<ArgumentException>();
    }

    // ── Expire ────────────────────────────────────────────────────────────────

    [Fact]
    public void Expire_on_Active_session_sets_Expired_status_and_raises_ExpiredEvent()
    {
        var session = CreateActiveSession();

        session.Expire(Now);

        session.Status.Should().Be(SessionStatus.Expired);
        session.EndedAt.Should().Be(Now);
        session.ShouldContainDomainEvent<LiveTrackingSessionExpiredDomainEvent>()
            .SessionId.Should().Be(session.Id);
    }

    [Fact]
    public void Expire_on_Paused_session_succeeds()
    {
        var session = CreateActiveSession();
        session.Pause(Now);
        session.ClearDomainEvents();

        session.Expire(Now.AddMinutes(1));

        session.Status.Should().Be(SessionStatus.Expired);
    }

    [Fact]
    public void Expire_on_Completed_session_throws_BusinessRuleViolationException()
    {
        var session = CreateActiveSession();
        session.End("Done.", Now);

        var act = () => session.Expire(Now.AddMinutes(1));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Expire_on_already_Expired_session_throws_BusinessRuleViolationException()
    {
        var session = CreateActiveSession();
        session.Expire(Now);

        var act = () => session.Expire(Now.AddMinutes(1));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ── AddCheckpoint ─────────────────────────────────────────────────────────

    [Fact]
    public void AddCheckpoint_adds_NotReached_checkpoint_to_Active_session()
    {
        var session = CreateActiveSession();

        session.AddCheckpoint(WaypointA);

        session.TourCheckpoints.Should().HaveCount(1);
        session.TourCheckpoints.First().WaypointId.Should().Be(WaypointA);
        session.TourCheckpoints.First().Status.Should().Be(CheckpointStatus.NotReached);
    }

    [Fact]
    public void AddCheckpoint_with_duplicate_waypointId_throws_BusinessRuleViolationException()
    {
        var session = CreateActiveSession();
        session.AddCheckpoint(WaypointA);

        var act = () => session.AddCheckpoint(WaypointA);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void AddCheckpoint_with_empty_guid_throws_ArgumentException()
    {
        var session = CreateActiveSession();

        var act = () => session.AddCheckpoint(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    // ── ReachCheckpoint ───────────────────────────────────────────────────────

    [Fact]
    public void ReachCheckpoint_transitions_NotReached_to_Reached_and_raises_event()
    {
        var session = CreateSessionWithCheckpoints();

        session.ReachCheckpoint(WaypointA, Now, "On time.");

        var checkpoint = session.TourCheckpoints.First(c => c.WaypointId == WaypointA);
        checkpoint.Status.Should().Be(CheckpointStatus.Reached);
        checkpoint.ReachedAt.Should().Be(Now);
        checkpoint.Notes.Should().Be("On time.");

        var evt = session.ShouldContainDomainEvent<CheckpointReachedDomainEvent>();
        evt.WaypointId.Should().Be(WaypointA);
    }

    [Fact]
    public void ReachCheckpoint_on_already_Reached_checkpoint_throws_BusinessRuleViolationException()
    {
        var session = CreateSessionWithCheckpoints();
        session.ReachCheckpoint(WaypointA, Now);
        session.ClearDomainEvents();

        var act = () => session.ReachCheckpoint(WaypointA, Now.AddMinutes(1));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void ReachCheckpoint_for_unknown_waypoint_throws_BusinessRuleViolationException()
    {
        var session = CreateSessionWithCheckpoints();
        var unknownWaypoint = Guid.NewGuid();

        var act = () => session.ReachCheckpoint(unknownWaypoint, Now);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ── SkipCheckpoint ────────────────────────────────────────────────────────

    [Fact]
    public void SkipCheckpoint_transitions_NotReached_to_Skipped_and_raises_event()
    {
        var session = CreateSessionWithCheckpoints();

        session.SkipCheckpoint(WaypointB, "Road closed.");

        var checkpoint = session.TourCheckpoints.First(c => c.WaypointId == WaypointB);
        checkpoint.Status.Should().Be(CheckpointStatus.Skipped);
        checkpoint.Notes.Should().Be("Road closed.");

        session.ShouldContainDomainEvent<CheckpointSkippedDomainEvent>()
            .WaypointId.Should().Be(WaypointB);
    }

    [Fact]
    public void SkipCheckpoint_on_Reached_checkpoint_throws_BusinessRuleViolationException()
    {
        var session = CreateSessionWithCheckpoints();
        session.ReachCheckpoint(WaypointA, Now);
        session.ClearDomainEvents();

        var act = () => session.SkipCheckpoint(WaypointA);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void SkipCheckpoint_on_already_Skipped_checkpoint_throws_BusinessRuleViolationException()
    {
        var session = CreateSessionWithCheckpoints();
        session.SkipCheckpoint(WaypointA, "First skip.");
        session.ClearDomainEvents();

        var act = () => session.SkipCheckpoint(WaypointA, "Second attempt.");

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ── Checkpoint operations require Active status ───────────────────────────

    [Fact]
    public void ReachCheckpoint_when_session_Paused_throws_BusinessRuleViolationException()
    {
        var session = CreateSessionWithCheckpoints();
        session.Pause(Now);
        session.ClearDomainEvents();

        var act = () => session.ReachCheckpoint(WaypointA, Now.AddSeconds(5));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void SkipCheckpoint_when_session_Completed_throws_BusinessRuleViolationException()
    {
        var session = CreateSessionWithCheckpoints();
        session.End("Done.", Now);
        session.ClearDomainEvents();

        var act = () => session.SkipCheckpoint(WaypointB);

        act.Should().Throw<BusinessRuleViolationException>();
    }
}
