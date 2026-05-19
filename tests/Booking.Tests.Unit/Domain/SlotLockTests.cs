using Booking.Domain.Entities;
using Booking.Domain.Events;
using FluentAssertions;
using YallaJo.SharedKernel.Domain.Exceptions;
using YallaJo.Tests.Shared;

namespace Booking.Tests.Unit.Domain;

/// <summary>
/// Unit tests for <see cref="SlotLock"/> value-tracking aggregate used during the 10-minute
/// payment window in the booking creation flow.
/// </summary>
public sealed class SlotLockTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SlotId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BookingId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Create_with_valid_inputs_returns_active_lock_with_correct_expiry()
    {
        var before = DateTime.UtcNow;

        var slotLock = SlotLock.Create(UserId, SlotId, participantCount: 2, ttl: TimeSpan.FromMinutes(10));

        slotLock.UserId.Should().Be(UserId);
        slotLock.AvailabilitySlotId.Should().Be(SlotId);
        slotLock.ParticipantCount.Should().Be(2);
        slotLock.IsReleased.Should().BeFalse();
        slotLock.ReleasedAt.Should().BeNull();
        slotLock.BookingId.Should().BeNull();
        slotLock.ExpiresAt.Should().BeCloseTo(before.AddMinutes(10), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_with_bookingId_sets_BookingId_field()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, 1, TimeSpan.FromMinutes(5), bookingId: BookingId);

        slotLock.BookingId.Should().Be(BookingId);
    }

    [Fact]
    public void Create_raises_SlotLockCreatedDomainEvent()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, 1, TimeSpan.FromMinutes(10));

        var evt = slotLock.ShouldContainDomainEvent<SlotLockCreatedDomainEvent>();
        evt.AvailabilitySlotId.Should().Be(SlotId);
        evt.UserId.Should().Be(UserId);
        evt.ExpiresAt.Should().Be(slotLock.ExpiresAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_throws_when_participantCount_invalid(int count)
    {
        var act = () => SlotLock.Create(UserId, SlotId, count, TimeSpan.FromMinutes(10));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Create_throws_when_UserId_empty()
    {
        var act = () => SlotLock.Create(Guid.Empty, SlotId, 1, TimeSpan.FromMinutes(10));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Create_throws_when_AvailabilitySlotId_empty()
    {
        var act = () => SlotLock.Create(UserId, Guid.Empty, 1, TimeSpan.FromMinutes(10));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Create_throws_when_TTL_not_positive()
    {
        var act = () => SlotLock.Create(UserId, SlotId, 1, TimeSpan.Zero);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== AttachBookingId =====

    [Fact]
    public void AttachBookingId_sets_field_when_initially_null()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, 1, TimeSpan.FromMinutes(10));

        slotLock.AttachBookingId(BookingId);

        slotLock.BookingId.Should().Be(BookingId);
    }

    [Fact]
    public void AttachBookingId_throws_when_BookingId_empty()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, 1, TimeSpan.FromMinutes(10));

        var act = () => slotLock.AttachBookingId(Guid.Empty);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void AttachBookingId_is_idempotent_when_called_with_same_BookingId()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, 1, TimeSpan.FromMinutes(10), bookingId: BookingId);

        var act = () => slotLock.AttachBookingId(BookingId);

        act.Should().NotThrow();
        slotLock.BookingId.Should().Be(BookingId);
    }

    [Fact]
    public void AttachBookingId_throws_when_attaching_a_different_BookingId()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, 1, TimeSpan.FromMinutes(10), bookingId: BookingId);
        var differentBooking = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var act = () => slotLock.AttachBookingId(differentBooking);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== Release =====

    [Fact]
    public void Release_marks_lock_released_and_sets_ReleasedAt()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, 1, TimeSpan.FromMinutes(10));

        slotLock.Release();

        slotLock.IsReleased.Should().BeTrue();
        slotLock.ReleasedAt.Should().NotBeNull();
    }

    [Fact]
    public void Release_raises_SlotLockReleasedDomainEvent()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, 1, TimeSpan.FromMinutes(10));

        slotLock.Release();

        slotLock.ShouldContainDomainEvent<SlotLockReleasedDomainEvent>();
    }

    [Fact]
    public void Release_is_idempotent()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, 1, TimeSpan.FromMinutes(10));
        slotLock.Release();
        var firstReleasedAt = slotLock.ReleasedAt;

        slotLock.Release();

        slotLock.ReleasedAt.Should().Be(firstReleasedAt);
    }

    // ===== IsExpired =====

    [Fact]
    public void IsExpired_returns_true_after_expires_when_not_released()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, 1, TimeSpan.FromMinutes(10));
        var future = slotLock.ExpiresAt.AddSeconds(1);

        slotLock.IsExpired(future).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_returns_false_before_expiry()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, 1, TimeSpan.FromMinutes(10));

        slotLock.IsExpired(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_returns_false_when_released_even_after_expires()
    {
        var slotLock = SlotLock.Create(UserId, SlotId, 1, TimeSpan.FromMinutes(10));
        slotLock.Release();
        var future = slotLock.ExpiresAt.AddSeconds(1);

        slotLock.IsExpired(future).Should().BeFalse();
    }
}
