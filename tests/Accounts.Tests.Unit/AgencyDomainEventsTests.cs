using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Events.Agency;
using FluentAssertions;

namespace Accounts.Tests.Unit;

/// <summary>
/// Domain-level tests for the notification-only domain events raised by the
/// <see cref="AgencyApplication"/> and <see cref="AgencyInvitation"/> aggregates
/// when an agency owner responds to an application or a guide responds to an
/// invitation (GAP-c).
///
/// Covers:
/// - Approve()/Reject() on AgencyApplication raise the correct event with payload.
/// - Accept()/Decline() on AgencyInvitation raise the correct event with payload.
/// - The early-return guard (Status != Pending) prevents duplicate/no-op events.
/// </summary>
public sealed class AgencyDomainEventsTests
{
    private static readonly Guid GuideUserId  = Guid.NewGuid();
    private static readonly Guid AgencyUserId = Guid.NewGuid();

    // ──────────────────────────────────────────────────────────────────────────
    // AgencyApplication.Approve
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_RaisesAgencyApplicationApprovedDomainEvent_WithPayload()
    {
        var app = AgencyApplication.Create(GuideUserId, AgencyUserId, "Please add me");

        app.Approve();

        app.Status.Should().Be(AgencyApplicationStatus.Approved);
        var evt = app.DomainEvents.OfType<AgencyApplicationApprovedDomainEvent>().Single();
        evt.ApplicationId.Should().Be(app.Id);
        evt.GuideUserId.Should().Be(GuideUserId);
        evt.AgencyUserId.Should().Be(AgencyUserId);
    }

    [Fact]
    public void Approve_WhenNotPending_DoesNotRaiseApprovedEvent()
    {
        var app = AgencyApplication.Create(GuideUserId, AgencyUserId, null);
        app.Approve(); // -> Approved

        app.Approve(); // no-op (guard)

        app.DomainEvents.OfType<AgencyApplicationApprovedDomainEvent>()
            .Should().ContainSingle("the early-return guard prevents a second approval event");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AgencyApplication.Reject
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_RaisesAgencyApplicationRejectedDomainEvent_WithTrimmedReason()
    {
        var app = AgencyApplication.Create(GuideUserId, AgencyUserId, null);

        app.Reject("  Incomplete profile  ");

        app.Status.Should().Be(AgencyApplicationStatus.Rejected);
        var evt = app.DomainEvents.OfType<AgencyApplicationRejectedDomainEvent>().Single();
        evt.ApplicationId.Should().Be(app.Id);
        evt.GuideUserId.Should().Be(GuideUserId);
        evt.AgencyUserId.Should().Be(AgencyUserId);
        evt.Reason.Should().Be("Incomplete profile");
    }

    [Fact]
    public void Reject_WhenNotPending_DoesNotRaiseRejectedEvent()
    {
        var app = AgencyApplication.Create(GuideUserId, AgencyUserId, null);
        app.Approve(); // -> Approved (no longer Pending)

        app.Reject("too late");

        app.DomainEvents.OfType<AgencyApplicationRejectedDomainEvent>()
            .Should().BeEmpty("a non-pending application cannot be rejected");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AgencyInvitation.Accept
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Accept_RaisesAgencyInvitationAcceptedDomainEvent_WithPayload()
    {
        var invitation = AgencyInvitation.Create(AgencyUserId, GuideUserId, "Join us", 15m);

        invitation.Accept();

        invitation.Status.Should().Be(AgencyInvitationStatus.Accepted);
        var evt = invitation.DomainEvents.OfType<AgencyInvitationAcceptedDomainEvent>().Single();
        evt.InvitationId.Should().Be(invitation.Id);
        evt.AgencyUserId.Should().Be(AgencyUserId);
        evt.GuideUserId.Should().Be(GuideUserId);
    }

    [Fact]
    public void Accept_WhenNotPending_DoesNotRaiseAcceptedEvent()
    {
        var invitation = AgencyInvitation.Create(AgencyUserId, GuideUserId, "Join us", 15m);
        invitation.Decline(); // -> Declined

        invitation.Accept();

        invitation.DomainEvents.OfType<AgencyInvitationAcceptedDomainEvent>()
            .Should().BeEmpty("a declined invitation cannot be accepted");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AgencyInvitation.Decline
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Decline_RaisesAgencyInvitationDeclinedDomainEvent_WithPayload()
    {
        var invitation = AgencyInvitation.Create(AgencyUserId, GuideUserId, "Join us", 15m);

        invitation.Decline();

        invitation.Status.Should().Be(AgencyInvitationStatus.Declined);
        var evt = invitation.DomainEvents.OfType<AgencyInvitationDeclinedDomainEvent>().Single();
        evt.InvitationId.Should().Be(invitation.Id);
        evt.AgencyUserId.Should().Be(AgencyUserId);
        evt.GuideUserId.Should().Be(GuideUserId);
    }

    [Fact]
    public void Decline_WhenNotPending_DoesNotRaiseDeclinedEvent()
    {
        var invitation = AgencyInvitation.Create(AgencyUserId, GuideUserId, "Join us", 15m);
        invitation.Accept(); // -> Accepted

        invitation.Decline();

        invitation.DomainEvents.OfType<AgencyInvitationDeclinedDomainEvent>()
            .Should().BeEmpty("an accepted invitation cannot be declined");
    }
}
