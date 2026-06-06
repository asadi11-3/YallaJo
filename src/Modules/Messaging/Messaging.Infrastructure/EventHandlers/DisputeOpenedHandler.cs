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
/// Consumes <see cref="DisputeOpenedIntegrationEvent"/> (logical name: finance.dispute-opened.v1)
/// and queues an in-app notification confirming the dispute was registered.
/// Mirrors the <c>PaymentCompletedHandler</c> pattern: idempotency via inbox store,
/// notification persisted through <see cref="MessagingDbContext.Notifications"/>.
/// </summary>
public sealed class DisputeOpenedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<DisputeOpenedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<DisputeOpenedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<DisputeOpenedIntegrationEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var evt = notification.Event;

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct).ConfigureAwait(false))
        {
            return;
        }

        if (evt.UserId == Guid.Empty)
        {
            logger.LogWarning(
                "DisputeOpenedIntegrationEvent {DisputeId} has no UserId; skipping notification creation.",
                evt.DisputeId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var notification1 = Notification.Create(
            userId: evt.UserId,
            type: NotificationType.DisputeOpened,
            channel: NotificationChannel.InApp,
            title: "Dispute Opened",
            body: $"Your dispute has been registered (reason: {evt.Reason}). Our support team will review it shortly.",
            priority: NotificationPriority.High,
            entityType: "Dispute",
            entityId: evt.DisputeId);

        dbContext.Notifications.Add(notification1);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Queued dispute opened notification for dispute {DisputeId} (payment {PaymentId}, user {UserId})",
            evt.DisputeId, evt.PaymentId, evt.UserId);
    }
}
