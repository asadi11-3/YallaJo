using Booking.Contracts.IntegrationEvents;
using Finance.Application.Interfaces;
using Finance.Contracts.Services;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Application.EventHandlers;

/// <summary>
/// Consumes <see cref="TourBookingCancelledIntegrationEvent"/> and, when a refund is owed
/// (RefundAmount &gt; 0), creates a child Refund-type Payment + eagerly invokes the gateway.
/// Per F-R5: Booking owns refund policy → it tells Finance the exact RefundAmount to execute.
/// </summary>
public sealed class BookingTourBookingCancelledHandler(
    IPaymentRepository paymentRepository,
    IFinanceInboxStore inboxStore,
    IFinanceUnitOfWork unitOfWork,
    IPaymentGateway gateway,
    TimeProvider timeProvider,
    ILogger<BookingTourBookingCancelledHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingCancelledIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourBookingCancelledIntegrationEvent> notification,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct).ConfigureAwait(false))
        {
            logger.LogWarning(
                "Finance: TourBookingCancelled message {MessageId} (BookingId={BookingId}) already processed — skipping.",
                notification.MessageId, notification.Event.BookingId);
            return;
        }

        var evt = notification.Event;

        if (evt.RefundAmount <= 0m)
        {
            logger.LogInformation(
                "Finance: TourBookingCancelled BookingId={BookingId} has 0 refund — no Refund row created.",
                evt.BookingId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var bookingPayments = await paymentRepository.GetByBookingIdAsync(evt.BookingId, ct).ConfigureAwait(false);
        var original = bookingPayments
            .FirstOrDefault(p => p.PaymentType == PaymentType.Booking && p.Status == PaymentStatus.Completed);

        if (original is null)
        {
            logger.LogWarning(
                "Finance: TourBookingCancelled BookingId={BookingId} — no Completed Booking payment found; cannot refund.",
                evt.BookingId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        // Re-attach the parent so EF tracks state transitions during ApplyRefundCompletion.
        var trackedOriginal = await paymentRepository.GetByIdAsync(original.Id, ct).ConfigureAwait(false);
        if (trackedOriginal is null)
        {
            logger.LogWarning("Finance: parent payment {PaymentId} disappeared between lookups — aborting.", original.Id);
            return;
        }

        var refundMoney = new Money(evt.RefundAmount, evt.Currency);
        var reason = string.IsNullOrWhiteSpace(evt.Reason)
            ? $"BookingCancellation:{evt.Source}"
            : $"BookingCancellation:{evt.Source}:{evt.Reason}";

        var createRefundResult = trackedOriginal.CreateRefund(refundMoney, reason, trackedOriginal.GatewayProvider, timeProvider);
        if (createRefundResult.IsFailure)
        {
            logger.LogWarning(
                "Finance: cannot create refund for payment {PaymentId}: {Error}.",
                trackedOriginal.Id,
                createRefundResult.Errors.FirstOrDefault()?.Message ?? "Unknown error");
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var refund = createRefundResult.Value!;

        await paymentRepository.AddAsync(refund, ct).ConfigureAwait(false);

        // Call gateway eagerly so cancellation refunds confirm in real-time.
        RefundResult? gatewayResult = null;
        try
        {
            gatewayResult = await gateway.RefundAsync(
                new RefundRequest(
                    OriginalGatewayPaymentId: trackedOriginal.GatewayTransactionId ?? trackedOriginal.TransactionId ?? string.Empty,
                    Amount: evt.RefundAmount,
                    Currency: evt.Currency,
                    Reason: reason),
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Finance: gateway RefundAsync threw for payment {PaymentId}.", trackedOriginal.Id);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (gatewayResult is null)
        {
            refund.MarkRefundFailed("Gateway threw exception.", nowUtc);
        }
        else if (gatewayResult.Status == RefundStatus.Completed)
        {
            var stampResult = refund.StampGatewayRefund(gatewayResult.GatewayRefundId);
            if (stampResult.IsFailure)
            {
                logger.LogWarning(
                    "Finance: cannot stamp refund {RefundId}: {Error}.",
                    refund.Id,
                    stampResult.Errors.FirstOrDefault()?.Message ?? "Unknown error");
            }
            refund.MarkRefundCompleted(gatewayResult.GatewayRefundId, nowUtc);
            trackedOriginal.ApplyRefundCompletion(refundMoney, nowUtc);
        }
        else if (gatewayResult.Status == RefundStatus.Failed)
        {
            refund.MarkRefundFailed(gatewayResult.FailureCode ?? "Gateway reported Failed.", nowUtc);
        }
        // RefundStatus.Pending → leave as Pending; webhook flow will finalise.

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Finance: refund created BookingId={BookingId} RefundId={RefundId} Amount={Amount} {Currency} Status={Status}.",
            evt.BookingId, refund.Id, evt.RefundAmount, evt.Currency, refund.Status);
    }
}
