using Booking.Contracts.IntegrationEvents;
using Finance.Application.Interfaces;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Finance.Application.EventHandlers;

/// <summary>
/// Consumes <see cref="TourBookingPaymentExpiredIntegrationEvent"/> and flags the
/// pending Payment as Failed with reason <c>Payment.Expired</c> per F-R12. Idempotent
/// via inbox; no-op if no Pending payment exists for the booking (e.g. user never
/// initiated payment, or already marked expired by another path).
/// </summary>
public sealed class BookingTourBookingPaymentExpiredHandler(
    IPaymentRepository paymentRepository,
    IFinanceInboxStore inboxStore,
    IFinanceUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<BookingTourBookingPaymentExpiredHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingPaymentExpiredIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourBookingPaymentExpiredIntegrationEvent> notification,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct).ConfigureAwait(false))
        {
            logger.LogWarning(
                "Finance: TourBookingPaymentExpired message {MessageId} (BookingId={BookingId}) already processed — skipping.",
                notification.MessageId, notification.Event.BookingId);
            return;
        }

        var evt = notification.Event;
        var bookingPayments = await paymentRepository.GetByBookingIdAsync(evt.BookingId, ct).ConfigureAwait(false);
        var pending = bookingPayments
            .FirstOrDefault(p => p.PaymentType == PaymentType.Booking && p.Status == PaymentStatus.Pending);

        if (pending is null)
        {
            logger.LogInformation(
                "Finance: TourBookingPaymentExpired BookingId={BookingId} — no Pending payment found; idempotent no-op.",
                evt.BookingId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        // Re-attach so EF tracks state transition.
        var tracked = await paymentRepository.GetByIdAsync(pending.Id, ct).ConfigureAwait(false);
        if (tracked is null)
        {
            logger.LogWarning(
                "Finance: Payment {PaymentId} disappeared between lookups — aborting expiry.",
                pending.Id);
            return;
        }

        tracked.MarkExpired(timeProvider.GetUtcNow().UtcDateTime);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Finance: Payment {PaymentId} for BookingId={BookingId} marked Expired (Payment.Expired).",
            tracked.Id, evt.BookingId);
    }
}
