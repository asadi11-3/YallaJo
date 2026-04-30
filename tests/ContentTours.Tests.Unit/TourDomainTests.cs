using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Events;
using FluentAssertions;
using YallaJo.Tests.Shared;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Pure domain tests for the Task-1 Tour state machine. No infrastructure, no DB —
/// asserts the aggregate enforces its own invariants and emits the right domain events.
/// </summary>
public sealed class TourDomainTests : DomainTestBase
{
    [Fact]
    public void Create_NewTourIsInDraftStatus()
    {
        var tour = TestTourFactory.CreateDraft();

        tour.Status.Should().Be(TourStatus.Draft);
        tour.IsDeleted.Should().BeFalse();
        tour.SubmittedAt.Should().BeNull();
        tour.ApprovedAt.Should().BeNull();
        tour.RejectedAt.Should().BeNull();
        tour.SuspendedAt.Should().BeNull();
        tour.ReinstatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_RaisesTourCreatedDomainEventWithIdentityFields()
    {
        var tour = TestTourFactory.CreateDraft(slug: "petra-tour", description: "Long enough description text.");

        var evt = DomainEventAssertions.ShouldContainDomainEvent<TourCreatedDomainEvent>(tour);
        evt.TourId.Should().Be(tour.Id);
        evt.Name.Should().Be(tour.Name);
        evt.Slug.Should().Be("petra-tour");
        evt.Description.Should().Be("Long enough description text.");
    }

    [Fact]
    public void Submit_TransitionsDraftToPendingAndStampsSubmittedAt()
    {
        var tour = TestTourFactory.CreateDraft();
        tour.ClearDomainEvents();

        tour.Submit();

        tour.Status.Should().Be(TourStatus.Pending);
        tour.SubmittedAt.Should().NotBeNull();
        DomainEventAssertions.ShouldContainDomainEvent<TourSubmittedDomainEvent>(tour);
    }

    [Fact]
    public void Submit_FromNonDraftThrowsInvalidOperation()
    {
        var tour = TestTourFactory.CreatePending();

        var act = () => tour.Submit();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*InvalidTransition*");
    }

    [Fact]
    public void Approve_TransitionsPendingToApprovedAndStampsAuditFields()
    {
        var reviewer = Guid.NewGuid();
        var tour = TestTourFactory.CreatePending();
        tour.ClearDomainEvents();

        tour.Approve(reviewer);

        tour.Status.Should().Be(TourStatus.Approved);
        tour.ApprovedAt.Should().NotBeNull();
        tour.ApprovedByUserId.Should().Be(reviewer);
        DomainEventAssertions.ShouldContainDomainEvent<TourApprovedDomainEvent>(tour);
    }

    [Fact]
    public void Reject_RequiresReasonAndStampsAuditFields()
    {
        var reviewer = Guid.NewGuid();
        var tour = TestTourFactory.CreatePending();
        tour.ClearDomainEvents();

        tour.Reject("Photos are blurry.", reviewer);

        tour.Status.Should().Be(TourStatus.Rejected);
        tour.RejectedByUserId.Should().Be(reviewer);
        tour.RejectionReason.Should().Be("Photos are blurry.");
        tour.RejectedAt.Should().NotBeNull();
        var evt = DomainEventAssertions.ShouldContainDomainEvent<TourRejectedDomainEvent>(tour);
        evt.Reason.Should().Be("Photos are blurry.");
    }

    [Fact]
    public void Reject_BlankReasonThrowsArgumentException()
    {
        var tour = TestTourFactory.CreatePending();

        var act = () => tour.Reject(" ", Guid.NewGuid());

        act.Should().Throw<ArgumentException>()
            .WithParameterName("reason");
    }

    [Fact]
    public void UpdateRejectedTour_ResetsToDraftAndClearsRejectionAudit()
    {
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateRejected(createdByUserId: owner);
        tour.Status.Should().Be(TourStatus.Rejected);
        tour.RejectionReason.Should().NotBeNull();

        tour.ClearDomainEvents();
        tour.Update(
            name:            tour.Name,
            slug:            tour.Slug,
            difficulty:      tour.Difficulty,
            durationMinutes: tour.DurationMinutes,
            maxGroupSize:    tour.MaxGroupSize,
            basePriceAmount: tour.BasePrice.Amount,
            currency:        tour.Currency,
            location:        tour.Location);

        tour.Status.Should().Be(TourStatus.Draft);
        tour.RejectionReason.Should().BeNull();
        tour.RejectedAt.Should().BeNull();
        tour.RejectedByUserId.Should().BeNull();
    }

    [Fact]
    public void Suspend_TransitionsApprovedToSuspendedAndStoresReason()
    {
        var tour = TestTourFactory.CreateApproved();
        tour.ClearDomainEvents();

        tour.Suspend("Repeat customer complaints.");

        tour.Status.Should().Be(TourStatus.Suspended);
        tour.SuspensionReason.Should().Be("Repeat customer complaints.");
        tour.SuspendedAt.Should().NotBeNull();
        DomainEventAssertions.ShouldContainDomainEvent<TourSuspendedDomainEvent>(tour);
    }

    [Fact]
    public void Reinstate_TransitionsSuspendedBackToApprovedAndClearsSuspension()
    {
        var tour = TestTourFactory.CreateSuspended();
        tour.ClearDomainEvents();

        tour.Reinstate();

        tour.Status.Should().Be(TourStatus.Approved);
        tour.SuspensionReason.Should().BeNull();
        tour.SuspendedAt.Should().BeNull();
        tour.ReinstatedAt.Should().NotBeNull();
        DomainEventAssertions.ShouldContainDomainEvent<TourReinstatedDomainEvent>(tour);
    }

    [Fact]
    public void SoftDelete_BlocksFurtherStateChanges()
    {
        var tour = TestTourFactory.CreateDraft();
        tour.SoftDelete();

        var act = () => tour.Submit();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Deleted*");
    }
}
