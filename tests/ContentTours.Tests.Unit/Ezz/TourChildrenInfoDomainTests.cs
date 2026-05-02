using ContentTours.Domain.Enums;
using ContentTours.Domain.Events;
using FluentAssertions;
using YallaJo.Tests.Shared;

namespace ContentTours.Tests.Unit.Ezz;

/// <summary>
/// Pure domain tests for Tour.ChildrenInfo behaviour (D.8).
/// Verifies aggregate invariants, replace-set semantics, mirror flag, and event emission.
/// </summary>
public sealed class TourChildrenInfoDomainTests : DomainTestBase
{
    // ── valid update ──────────────────────────────────────────────────────────

    [Fact]
    public void UpdateChildrenInfo_ValidInput_UpdatesAgesAndFacilities()
    {
        var tour = TestTourFactory.CreateDraft();

        tour.UpdateChildrenInfo(
            allowsChildren: true,
            minChildAge: 5,
            maxChildAge: 10,
            childFacilities: [ChildFacility.PlayArea, ChildFacility.Stroller, ChildFacility.PlayArea]);

        tour.AllowsChildren.Should().BeTrue();
        tour.MinChildAge.Should().Be(5);
        tour.MaxChildAge.Should().Be(10);
        tour.ChildFacilities.Select(x => x.Facility).Should().Equal(
            ChildFacility.Stroller,
            ChildFacility.PlayArea);
    }

    // ── allowsChildren=false clears data ─────────────────────────────────────

    [Fact]
    public void UpdateChildrenInfo_AllowsChildrenFalse_ClearsAgesAndFacilities()
    {
        var tour = TestTourFactory.CreateDraft();
        tour.UpdateChildrenInfo(
            allowsChildren: true,
            minChildAge: 4,
            maxChildAge: 7,
            childFacilities: [ChildFacility.Stroller, ChildFacility.ChildSeat]);

        tour.UpdateChildrenInfo(
            allowsChildren: false,
            minChildAge: 3,
            maxChildAge: 6,
            childFacilities: [ChildFacility.PlayArea]);

        tour.AllowsChildren.Should().BeFalse();
        tour.MinChildAge.Should().BeNull();
        tour.MaxChildAge.Should().BeNull();
        tour.ChildFacilities.Should().BeEmpty();
    }

    // ── replace-set semantics across two PUTs ────────────────────────────────

    [Fact]
    public void UpdateChildrenInfo_TwoSuccessivePUTsWithDifferentFacilities_ReplacesSet()
    {
        var tour = TestTourFactory.CreateDraft();

        tour.UpdateChildrenInfo(
            allowsChildren: true,
            minChildAge:    4,
            maxChildAge:    9,
            childFacilities: [ChildFacility.Stroller, ChildFacility.PlayArea]);

        tour.UpdateChildrenInfo(
            allowsChildren: true,
            minChildAge:    4,
            maxChildAge:    9,
            childFacilities: [ChildFacility.ChildSeat]);

        tour.ChildFacilities.Should().HaveCount(1);
        tour.ChildFacilities.Single().Facility.Should().Be(ChildFacility.ChildSeat);
    }

    // ── invalid ages ──────────────────────────────────────────────────────────

    [Fact]
    public void UpdateChildrenInfo_MinChildAgeBelowZero_Throws()
    {
        var tour = TestTourFactory.CreateDraft();

        var act = () => tour.UpdateChildrenInfo(
            allowsChildren: true,
            minChildAge: -1,
            maxChildAge: 6,
            childFacilities: [ChildFacility.Stroller]);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateChildrenInfo_MaxChildAgeAbove18_Throws()
    {
        var tour = TestTourFactory.CreateDraft();

        var act = () => tour.UpdateChildrenInfo(
            allowsChildren: true,
            minChildAge: 7,
            maxChildAge: 19,
            childFacilities: [ChildFacility.Stroller]);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateChildrenInfo_MaxChildAgeLowerThanMin_Throws()
    {
        var tour = TestTourFactory.CreateDraft();

        var act = () => tour.UpdateChildrenInfo(
            allowsChildren: true,
            minChildAge: 10,
            maxChildAge: 9,
            childFacilities: [ChildFacility.Stroller]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tour.ChildrenInfoInvalid*");
    }

    // ── min == max allowed ────────────────────────────────────────────────────

    [Fact]
    public void UpdateChildrenInfo_MinEqualsMax_IsAllowed()
    {
        var tour = TestTourFactory.CreateDraft();

        var act = () => tour.UpdateChildrenInfo(
            allowsChildren: true,
            minChildAge: 5,
            maxChildAge: 5,
            childFacilities: [ChildFacility.Stroller]);

        act.Should().NotThrow();
        tour.MinChildAge.Should().Be(5);
        tour.MaxChildAge.Should().Be(5);
    }

    // ── domain event ──────────────────────────────────────────────────────────

    [Fact]
    public void UpdateChildrenInfo_RaisesTourUpdatedDomainEventWithChildrenInfoChangedTrue()
    {
        var tour = TestTourFactory.CreateDraft();
        tour.ClearDomainEvents();

        tour.UpdateChildrenInfo(
            allowsChildren: true,
            minChildAge: 3,
            maxChildAge: 8,
            childFacilities: [ChildFacility.Stroller]);

        var evt = DomainEventAssertions.ShouldContainDomainEvent<TourUpdatedDomainEvent>(tour);
        evt.ChildrenInfoChanged.Should().BeTrue();
    }

    // ── IsChildFriendly mirror ────────────────────────────────────────────────

    [Fact]
    public void UpdateChildrenInfo_TracksLegacyIsChildFriendlyMirror()
    {
        var tour = TestTourFactory.CreateDraft();

        tour.UpdateChildrenInfo(
            allowsChildren: true,
            minChildAge: 4,
            maxChildAge: 10,
            childFacilities: [ChildFacility.Stroller]);

        tour.AllowsChildren.Should().BeTrue();
        tour.IsChildFriendly.Should().BeTrue();

        tour.UpdateChildrenInfo(
            allowsChildren: false,
            minChildAge: null,
            maxChildAge: null,
            childFacilities: null);

        tour.AllowsChildren.Should().BeFalse();
        tour.IsChildFriendly.Should().BeFalse();
    }
}
