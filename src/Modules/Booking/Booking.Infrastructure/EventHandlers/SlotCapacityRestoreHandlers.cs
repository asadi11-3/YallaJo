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
                slot.ReleaseLock(e.ParticipantCount);
                logger.LogInformation(
                    "Released {Count} locked seat(s) on slot {SlotId} after cancel of booking {BookingId}",
                    e.ParticipantCount,
                    slot.Id,
                    e.BookingId);
                break;
            case BookingStatus.Confirmed:
                slot.ReleaseBooking(e.ParticipantCount);
                logger.LogInformation(
                    "Released {Count} booked seat(s) on slot {SlotId} after cancel of confirmed booking {BookingId}",
                    e.ParticipantCount,
                    slot.Id,
                    e.BookingId);
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

        slot.ReleaseLock(e.ParticipantCount);
        logger.LogInformation(
            "Released {Count} locked seat(s) on slot {SlotId} after rejection of booking {BookingId}",
            e.ParticipantCount,
            slot.Id,
            e.BookingId);
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

        slot.ReleaseLock(e.ParticipantCount);
        logger.LogInformation(
            "Released {Count} locked seat(s) on slot {SlotId} after payment expiry of booking {BookingId}",
            e.ParticipantCount,
            slot.Id,
            e.BookingId);
    }
}
