using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Infrastructure.EventHandlers;

/// <summary>
/// Restores availability-slot capacity when a booking is cancelled.
/// Reads <see cref="TourBookingCancelledDomainEvent.PreviousStatus"/> to decide whether to release
/// a soft lock (was AwaitingPayment/PendingConfirmation) or a hard seat (was Confirmed).
/// MUST NOT call SaveChanges; the change tracker piggy-backs on the originating command's UoW.
/// </summary>
internal sealed class RestoreSlotCapacityOnCancelHandler(
    IAvailabilitySlotRepository slotRepository,
    ILogger<RestoreSlotCapacityOnCancelHandler> logger)
    : INotificationHandler<DomainEventNotification<TourBookingCancelledDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourBookingCancelledDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var slot = await slotRepository.GetByIdWithLockAsync(e.AvailabilitySlotId, cancellationToken).ConfigureAwait(false);
        if (slot is null)
        {
            logger.LogWarning(
                "Cancel restore skipped: AvailabilitySlot {SlotId} not found for booking {BookingId}",
                e.AvailabilitySlotId,
                e.BookingId);
            return;
        }

        switch (e.PreviousStatus)
        {
            case BookingStatus.AwaitingPayment:
            case BookingStatus.PendingConfirmation:
                // F24: release only what is actually locked. The lock may already have been
                // released (e.g. by the auto-expire background service) before this cancel was
                // processed; calling ReleaseLock unconditionally throws inside the UoW dispatch
                // and surfaces as a 500. Clamp to keep the restore idempotent.
                SlotCapacityRestore.ReleaseLockSafely(slot, e.ParticipantCount, e.BookingId, logger);
                break;
            case BookingStatus.Confirmed:
                SlotCapacityRestore.ReleaseBookingSafely(slot, e.ParticipantCount, e.BookingId, logger);
                break;
            default:
                logger.LogWarning(
                    "Cancel restore skipped: unexpected previous status {Status} for booking {BookingId}",
                    e.PreviousStatus,
                    e.BookingId);
                break;
        }
    }
}

/// <summary>
/// Restores capacity when a provider rejects a booking. Rejection only happens from
/// <see cref="BookingStatus.PendingConfirmation"/>, so we always release a soft lock.
/// </summary>
internal sealed class RestoreSlotCapacityOnRejectHandler(
    IAvailabilitySlotRepository slotRepository,
    ILogger<RestoreSlotCapacityOnRejectHandler> logger)
    : INotificationHandler<DomainEventNotification<TourBookingRejectedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourBookingRejectedDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var slot = await slotRepository.GetByIdWithLockAsync(e.AvailabilitySlotId, cancellationToken).ConfigureAwait(false);
        if (slot is null)
        {
            logger.LogWarning(
                "Reject restore skipped: AvailabilitySlot {SlotId} not found for booking {BookingId}",
                e.AvailabilitySlotId,
                e.BookingId);
            return;
        }

        // F24: idempotent restore — clamp to actually-locked seats.
        SlotCapacityRestore.ReleaseLockSafely(slot, e.ParticipantCount, e.BookingId, logger);
    }
}

/// <summary>
/// Restores capacity when a booking auto-expires due to unpaid payment window.
/// Expire only happens from <see cref="BookingStatus.AwaitingPayment"/>, so we always release a soft lock.
/// </summary>
internal sealed class RestoreSlotCapacityOnExpireHandler(
    IAvailabilitySlotRepository slotRepository,
    ILogger<RestoreSlotCapacityOnExpireHandler> logger)
    : INotificationHandler<DomainEventNotification<TourBookingPaymentExpiredDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourBookingPaymentExpiredDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var slot = await slotRepository.GetByIdWithLockAsync(e.AvailabilitySlotId, cancellationToken).ConfigureAwait(false);
        if (slot is null)
        {
            logger.LogWarning(
                "Expire restore skipped: AvailabilitySlot {SlotId} not found for booking {BookingId}",
                e.AvailabilitySlotId,
                e.BookingId);
            return;
        }

        // F24: idempotent restore — clamp to actually-locked seats.
        SlotCapacityRestore.ReleaseLockSafely(slot, e.ParticipantCount, e.BookingId, logger);
    }
}

/// <summary>
/// Shared idempotent capacity-restore helpers (F24). Releasing slot capacity must never
/// throw if the lock/booking was already released by another flow (auto-expire vs. manual
/// cancel race), because these run inside the originating command's UoW dispatch and any
/// throw would roll the whole operation into a 500.
/// </summary>
internal static class SlotCapacityRestore
{
    public static void ReleaseLockSafely(AvailabilitySlot slot, int participantCount, Guid bookingId, ILogger logger)
    {
        var toRelease = Math.Min(participantCount, slot.LockedCount);
        if (toRelease > 0)
        {
            slot.ReleaseLock(toRelease);
            logger.LogInformation(
                "Released {Count} locked seat(s) on slot {SlotId} for booking {BookingId}",
                toRelease,
                slot.Id,
                bookingId);
        }

        if (toRelease < participantCount)
        {
            logger.LogWarning(
                "Slot {SlotId}: requested release of {Requested} locked seat(s) for booking {BookingId} but only {Released} were locked; clamped (idempotent restore).",
                slot.Id,
                participantCount,
                bookingId,
                toRelease);
        }
    }

    public static void ReleaseBookingSafely(AvailabilitySlot slot, int participantCount, Guid bookingId, ILogger logger)
    {
        var toRelease = Math.Min(participantCount, slot.BookedCount);
        if (toRelease > 0)
        {
            slot.ReleaseBooking(toRelease);
            logger.LogInformation(
                "Released {Count} booked seat(s) on slot {SlotId} for confirmed booking {BookingId}",
                toRelease,
                slot.Id,
                bookingId);
        }

        if (toRelease < participantCount)
        {
            logger.LogWarning(
                "Slot {SlotId}: requested release of {Requested} booked seat(s) for booking {BookingId} but only {Released} were booked; clamped (idempotent restore).",
                slot.Id,
                participantCount,
                bookingId,
                toRelease);
        }
    }
}
