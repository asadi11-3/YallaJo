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
/// Consumes <see cref="DisputeEscalatedIntegrationEvent"/> (logical name: finance.dispute-escalated.v1)
/// and queues a CRITICAL in-app notification telling the user that their dispute is receiving
/// higher-tier review. Mirrors the <c>DisputeOpenedHandler</c> pattern. Phase-3 WS-4.
/// </summary>
public sealed class DisputeEscalatedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<DisputeEscalatedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<DisputeEscalatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<DisputeEscalatedIntegrationEvent> notification, CancellationToken ct)
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
                "DisputeEscalatedIntegrationEvent {DisputeId} has no UserId; skipping notification creation.",
                evt.DisputeId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var reasonSnippet = evt.Reason.Length <= 200 ? evt.Reason : evt.Reason[..200] + "…";

        var entry = Notification.Create(
            userId: evt.UserId,
            type: NotificationType.DisputeEscalated,
            channel: NotificationChannel.InApp,
            title: "Dispute Escalated",
            body: $"Your dispute has been escalated to senior support. Reason: {reasonSnippet}",
            priority: NotificationPriority.High,
            entityType: "Dispute",
            entityId: evt.DisputeId);

        dbContext.Notifications.Add(entry);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Queued dispute escalated notification for dispute {DisputeId} (payment {PaymentId}, user {UserId}, admin {AdminId})",
            evt.DisputeId, evt.PaymentId, evt.UserId, evt.EscalatedByAdminId);
    }
}
