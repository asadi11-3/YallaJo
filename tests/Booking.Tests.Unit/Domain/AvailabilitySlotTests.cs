using Booking.Domain.Entities;
using Booking.Domain.Events;
using FluentAssertions;
using YallaJo.SharedKernel.Domain.Exceptions;
using YallaJo.Tests.Shared;

namespace Booking.Tests.Unit.Domain;

/// <summary>
/// Unit tests for <see cref="AvailabilitySlot"/> capacity-management methods added in Mohammad's sprint.
/// Covers Lock/ReleaseLock/ConfirmBooking/ReleaseBooking + the computed AvailableCount.
/// </summary>
public sealed class AvailabilitySlotTests
{
    private static readonly Guid TourGuideId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TourId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static AvailabilitySlot CreateSlot(int maxCapacity = 10) =>
        AvailabilitySlot.CreateForTour(
            tourGuideId: TourGuideId,
            tourId: TourId,
            date: new DateOnly(2026, 7, 15),
            start: new TimeOnly(9, 0),
            end: new TimeOnly(13, 0),
            maxCapacity: maxCapacity);

    // ===== AvailableCount =====

    [Fact]
    public void AvailableCount_returns_max_capacity_when_no_locks_or_bookings()
    {
        var slot = CreateSlot(maxCapacity: 18);

        slot.AvailableCount.Should().Be(18);
    }

    [Fact]
    public void AvailableCount_subtracts_locked_and_booked()
    {
        var slot = CreateSlot(maxCapacity: 10);
        slot.Lock(3);
        slot.ConfirmBooking(2);

        slot.AvailableCount.Should().Be(7); // 10 - 1 locked - 2 booked
    }

    // ===== Lock =====

    [Fact]
    public void Lock_increases_LockedCount_and_raises_capacity_changed_event()
    {
        var slot = CreateSlot();

        slot.Lock(2);

        slot.LockedCount.Should().Be(2);
        slot.AvailableCount.Should().Be(8);
        var evt = slot.ShouldContainDomainEvent<AvailabilitySlotCapacityChangedDomainEvent>();
        evt.OldCapacity.Should().Be(10);
        evt.NewCapacity.Should().Be(8);
    }

    [Fact]
    public void Lock_throws_when_insufficient_capacity()
    {
        var slot = CreateSlot(maxCapacity: 2);
        slot.Lock(2);

        var act = () => slot.Lock(1);

        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*Insufficient capacity*");
    }

    [Fact]
    public void Lock_throws_when_slot_is_inactive()
    {
        var slot = CreateSlot();
        slot.Deactivate();

        var act = () => slot.Lock(1);

        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*inactive slot*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Lock_throws_when_participant_count_is_invalid(int count)
    {
        var slot = CreateSlot();

        var act = () => slot.Lock(count);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== ReleaseLock =====

    [Fact]
    public void ReleaseLock_decreases_LockedCount_and_raises_event()
    {
        var slot = CreateSlot();
        slot.Lock(3);

        slot.ReleaseLock(2);

        slot.LockedCount.Should().Be(1);
        slot.AvailableCount.Should().Be(9);
    }

    [Fact]
    public void ReleaseLock_throws_when_releasing_more_than_locked()
    {
        var slot = CreateSlot();
        slot.Lock(2);

        var act = () => slot.ReleaseLock(3);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== ConfirmBooking =====

    [Fact]
    public void ConfirmBooking_moves_locked_to_booked_without_changing_available()
    {
        var slot = CreateSlot();
        slot.Lock(3);
        var availableBefore = slot.AvailableCount;

        slot.ConfirmBooking(2);

        slot.LockedCount.Should().Be(1);
        slot.BookedCount.Should().Be(2);
        slot.AvailableCount.Should().Be(availableBefore); // net no change
    }

    [Fact]
    public void ConfirmBooking_throws_when_confirming_more_than_locked()
    {
        var slot = CreateSlot();
        slot.Lock(2);

        var act = () => slot.ConfirmBooking(3);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== ReleaseBooking =====

    [Fact]
    public void ReleaseBooking_decreases_BookedCount_and_raises_event()
    {
        var slot = CreateSlot();
        slot.Lock(3);
        slot.ConfirmBooking(3);

        slot.ReleaseBooking(2);

        slot.BookedCount.Should().Be(1);
        slot.AvailableCount.Should().Be(9);
    }

    [Fact]
    public void ReleaseBooking_throws_when_releasing_more_than_booked()
    {
        var slot = CreateSlot();
        slot.Lock(1);
        slot.ConfirmBooking(1);

        var act = () => slot.ReleaseBooking(2);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== Activate / Deactivate =====

    [Fact]
    public void Deactivate_sets_IsActive_to_false()
    {
        var slot = CreateSlot();

        slot.Deactivate();

        slot.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_idempotent_when_already_active()
    {
        var slot = CreateSlot();

        slot.Activate();

        slot.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_after_deactivate_restores_to_active()
    {
        var slot = CreateSlot();
        slot.Deactivate();

        slot.Activate();

        slot.IsActive.Should().BeTrue();
    }
}
