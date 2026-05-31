using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Domain.Repositories;
using Booking.Infrastructure.EventHandlers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Tests.Unit.EventHandlers;

/// <summary>
/// Phase 1 capacity fix: confirming a booking must convert locked seats to booked seats so that
/// a later cancel of the confirmed booking can release booked seats without throwing.
/// </summary>
public sealed class RestoreSlotCapacityOnConfirmHandlerTests
{
    private static readonly Guid TourGuideId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TourId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SlotId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid BookingId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid UserId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid ProviderId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private static AvailabilitySlot CreateLockedSlot(int maxCapacity, int locked)
    {
        var slot = AvailabilitySlot.CreateForTour(
            tourGuideId: TourGuideId,
            tourId: TourId,
            date: new DateOnly(2026, 7, 15),
            start: new TimeOnly(9, 0),
            end: new TimeOnly(13, 0),
            maxCapacity: maxCapacity);
        slot.Lock(locked);
        return slot;
    }

    private static DomainEventNotification<TourBookingConfirmedDomainEvent> ConfirmNotification(int participantCount) =>
        new(new TourBookingConfirmedDomainEvent(
            BookingId: BookingId,
            UserId: UserId,
            TourId: TourId,
            ProviderId: ProviderId,
            AvailabilitySlotId: SlotId,
            ParticipantCount: participantCount,
            ConfirmedAt: DateTime.UtcNow,
            Source: ConfirmationSource.PaymentWebhook));

    [Fact]
    public async Task Handle_converts_locked_seats_to_booked()
    {
        var slot = CreateLockedSlot(maxCapacity: 10, locked: 2);
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        repo.GetByIdWithLockAsync(SlotId, Arg.Any<CancellationToken>()).Returns(slot);

        var handler = new RestoreSlotCapacityOnConfirmHandler(
            repo, NullLogger<RestoreSlotCapacityOnConfirmHandler>.Instance);

        await handler.Handle(ConfirmNotification(participantCount: 2), CancellationToken.None);

        slot.LockedCount.Should().Be(0);
        slot.BookedCount.Should().Be(2);
        slot.AvailableCount.Should().Be(8); // unchanged net availability
    }

    [Fact]
    public async Task Handle_does_not_save_changes_directly()
    {
        // The handler must piggy-back on the originating command's UoW; it has no UoW dependency
        // at all, which structurally guarantees it cannot call SaveChanges. This test documents
        // that contract: a single repository read + an in-memory domain mutation only.
        var slot = CreateLockedSlot(maxCapacity: 5, locked: 1);
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        repo.GetByIdWithLockAsync(SlotId, Arg.Any<CancellationToken>()).Returns(slot);

        var handler = new RestoreSlotCapacityOnConfirmHandler(
            repo, NullLogger<RestoreSlotCapacityOnConfirmHandler>.Instance);

        await handler.Handle(ConfirmNotification(participantCount: 1), CancellationToken.None);

        await repo.Received(1).GetByIdWithLockAsync(SlotId, Arg.Any<CancellationToken>());
        slot.BookedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_is_safe_when_slot_not_found()
    {
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        repo.GetByIdWithLockAsync(SlotId, Arg.Any<CancellationToken>()).Returns((AvailabilitySlot?)null);

        var handler = new RestoreSlotCapacityOnConfirmHandler(
            repo, NullLogger<RestoreSlotCapacityOnConfirmHandler>.Instance);

        var act = async () => await handler.Handle(ConfirmNotification(participantCount: 2), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Confirmed_then_cancelled_releases_booked_seats_without_throwing()
    {
        // End-to-end capacity invariant: lock -> confirm (lock->seat) -> cancel-of-confirmed (release seat).
        // Before the Phase 1 fix, the confirm step never converted the lock, so ReleaseBooking threw.
        var slot = CreateLockedSlot(maxCapacity: 10, locked: 2);
        var repo = Substitute.For<IAvailabilitySlotRepository>();
        repo.GetByIdWithLockAsync(SlotId, Arg.Any<CancellationToken>()).Returns(slot);

        var confirmHandler = new RestoreSlotCapacityOnConfirmHandler(
            repo, NullLogger<RestoreSlotCapacityOnConfirmHandler>.Instance);
        await confirmHandler.Handle(ConfirmNotification(participantCount: 2), CancellationToken.None);

        slot.BookedCount.Should().Be(2);

        // Now cancel the confirmed booking — must release a booked seat (was Confirmed).
        var cancelHandler = new RestoreSlotCapacityOnCancelHandler(
            repo, NullLogger<RestoreSlotCapacityOnCancelHandler>.Instance);

        var cancelNotification = new DomainEventNotification<TourBookingCancelledDomainEvent>(
            new TourBookingCancelledDomainEvent(
                BookingId: BookingId,
                UserId: UserId,
                TourId: TourId,
                ProviderId: ProviderId,
                AvailabilitySlotId: SlotId,
                ParticipantCount: 2,
                PreviousStatus: BookingStatus.Confirmed,
                CancelledAt: DateTime.UtcNow,
                Source: CancellationSource.User,
                Reason: null,
                RefundAmount: 0m,
                Currency: "JOD"));

        var act = async () => await cancelHandler.Handle(cancelNotification, CancellationToken.None);

        await act.Should().NotThrowAsync();
        slot.BookedCount.Should().Be(0);
        slot.LockedCount.Should().Be(0);
        slot.AvailableCount.Should().Be(10);
    }
}
