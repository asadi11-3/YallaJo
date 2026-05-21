using Finance.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>
/// Consumes <see cref="RefundFailedIntegrationEvent"/>. The event lacks a direct UserId so this
/// handler logs an admin-facing warning and marks the inbox row processed. A future enrichment
/// step (e.g. a Finance->Messaging UserId lookup via a snapshot table) would let us push a
/// targeted notification to the user; for now this prevents unbounded retries and surfaces
/// the failure in logs / metrics for operators to act on manually.
/// </summary>
public sealed class RefundFailedHandler(
    IMessagingInboxStore inboxStore,
    IMessagingUnitOfWork unitOfWork,
    ILogger<RefundFailedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<RefundFailedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<RefundFailedIntegrationEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        logger.LogWarning(
            "Messaging: Refund FAILED for RefundId={RefundId} OriginalPaymentId={OriginalPaymentId} BookingId={BookingId} Amount={Amount} {Currency} Attempt={Attempt} Reason={Reason}",
            evt.RefundPaymentId, evt.OriginalPaymentId, evt.BookingId,
            evt.Amount, evt.Currency, evt.AttemptCount, evt.FailureReason);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
