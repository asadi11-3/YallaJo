using Booking.Application.Interfaces;
using Booking.Domain.Enums;
using Booking.Infrastructure.Persistence;
using Finance.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Infrastructure.EventHandlers;

/// <summary>
/// Phase 2: wires Finance's <see cref="PaymentCompletedIntegrationEvent"/> into the Booking lifecycle.
///
/// <para>
/// On successful payment, advances an <see cref="BookingStatus.AwaitingPayment"/> booking:
/// instant-bookable tours go straight to <see cref="BookingStatus.Confirmed"/>
/// (<see cref="ConfirmationSource.PaymentWebhook"/>, which triggers the Phase 1 capacity
/// conversion via <c>TourBookingConfirmedDomainEvent</c>); non-instant tours move to
/// <see cref="BookingStatus.PendingConfirmation"/> to await provider/auto-accept confirmation.
/// </para>
///
/// <para>
/// The Booking module has no InboxStore, so this handler is idempotent via the
/// <see cref="BookingStatus.AwaitingPayment"/> state guard: duplicate deliveries (or already
/// confirmed/cancelled/expired/rejected bookings) are safe no-ops. A missing booking is treated
/// as a <em>retryable</em> failure (thrown) rather than silently dropped, so the outbox redelivers.
/// </para>
/// </summary>
public sealed class PaymentCompletedConfirmBookingHandler(
    BookingDbContext dbContext,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<PaymentCompletedConfirmBookingHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PaymentCompletedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<PaymentCompletedIntegrationEvent> notification,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var evt = notification.Event;

        var booking = await dbContext.TourBookings
            .FirstOrDefaultAsync(b => b.Id == evt.BookingId, ct)
            .ConfigureAwait(false);

        if (booking is null)
        {
            // Cross-module ordering is not guaranteed; the booking row may not be visible yet.
            // Throw so the outbox treats this as retryable rather than silently dropping the
            // payment confirmation. Do NOT mark anything as processed.
            logger.LogWarning(
                "Booking: PaymentCompleted for unknown BookingId={BookingId} (PaymentId={PaymentId}); will retry.",
                evt.BookingId, evt.PaymentId);
            throw new InvalidOperationException(
                $"Booking {evt.BookingId} not found while handling PaymentCompleted (PaymentId={evt.PaymentId}).");
        }

        // Idempotency / safety guard: only an AwaitingPayment booking advances. This covers
        // duplicate PaymentCompleted, already-confirmed, pending-confirmation, cancelled,
        // expired, and rejected bookings — all safe no-ops.
        if (booking.Status != BookingStatus.AwaitingPayment)
        {
            logger.LogInformation(
                "Booking: PaymentCompleted ignored for Booking {BookingId} in state {Status} (PaymentId={PaymentId}).",
                booking.Id, booking.Status, evt.PaymentId);
            return;
        }

        if (booking.IsInstantBooking)
        {
            // Instant tour → confirm immediately. Raises TourBookingConfirmedDomainEvent, which
            // the Phase 1 RestoreSlotCapacityOnConfirmHandler converts locked seats to booked.
            booking.Confirm(ConfirmationSource.PaymentWebhook);
            logger.LogInformation(
                "Booking {BookingId} confirmed via PaymentWebhook (instant). PaymentId={PaymentId}.",
                booking.Id, evt.PaymentId);
        }
        else
        {
            // Non-instant tour → await provider/auto-accept confirmation. No domain event is
            // raised here; seats remain locked until the eventual Confirm.
            booking.MoveToPendingConfirmation();
            logger.LogInformation(
                "Booking {BookingId} moved to PendingConfirmation after payment (non-instant). PaymentId={PaymentId}.",
                booking.Id, evt.PaymentId);
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        // Cache invalidation mirrors ConfirmTourBookingCommandHandler (ERR-010: most specific tags).
        await cache.RemoveByTagAsync($"booking:{booking.Id:D}", ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync($"bookings:user:{booking.UserId:D}", ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync("bookings:admin", ct).ConfigureAwait(false);
    }
}
