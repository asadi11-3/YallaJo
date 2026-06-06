using Finance.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>
/// Consumer of <see cref="RefundCompletedIntegrationEvent"/>. Delivers an in-app refund-confirmation
/// notification to the traveler whose original payment was refunded. The Finance publisher enriches
/// UserId from the OriginalPayment lookup; if it is <see cref="Guid.Empty"/> (an in-flight
/// pre-enrichment outbox message) this handler logs a warning and skips notification creation,
/// matching the prior <see cref="RefundInitiatedHandler"/> behavior.
/// </summary>
public sealed class RefundCompletedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<RefundCompletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<RefundCompletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<RefundCompletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        if (evt.UserId == Guid.Empty)
        {
            logger.LogWarning(
                "RefundCompletedIntegrationEvent {RefundPaymentId} has no UserId (pre-enrichment in-flight message); skipping notification creation.",
                evt.RefundPaymentId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        dbContext.Notifications.Add(Notification.Create(
            evt.UserId,
            NotificationType.RefundCompleted,
            NotificationChannel.InApp,
            "Refund Issued",
            $"A refund of {evt.Amount} {evt.Currency} has been processed for your booking.",
            NotificationPriority.High,
            entityType: "Refund",
            entityId: evt.RefundPaymentId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation(
            "Queued refund completed notification for refund {RefundPaymentId} (booking {BookingId})",
            evt.RefundPaymentId, evt.BookingId);
    }
}
