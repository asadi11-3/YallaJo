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
/// Consumes <see cref="DisputeResolvedIntegrationEvent"/> (logical name: finance.dispute-resolved.v1)
/// and queues an in-app notification informing the user of the resolution outcome.
/// Mirrors the <c>DisputeOpenedHandler</c> pattern (Phase-2 WS-3b): single recipient, idempotent via inbox store.
/// Phase-3 WS-4.
/// </summary>
public sealed class DisputeResolvedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<DisputeResolvedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<DisputeResolvedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<DisputeResolvedIntegrationEvent> notification, CancellationToken ct)
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
                "DisputeResolvedIntegrationEvent {DisputeId} has no UserId; skipping notification creation.",
                evt.DisputeId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var notesSnippet = string.IsNullOrWhiteSpace(evt.ResolutionNotes)
            ? string.Empty
            : (evt.ResolutionNotes!.Length <= 200 ? evt.ResolutionNotes! : evt.ResolutionNotes![..200] + "…");
        var body = string.IsNullOrEmpty(notesSnippet)
            ? $"Your dispute has been resolved (outcome: {evt.Resolution})."
            : $"Your dispute has been resolved (outcome: {evt.Resolution}). {notesSnippet}";

        var entry = Notification.Create(
            userId: evt.UserId,
            type: NotificationType.DisputeResolved,
            channel: NotificationChannel.InApp,
            title: "Dispute Resolved",
            body: body,
            priority: NotificationPriority.Medium,
            entityType: "Dispute",
            entityId: evt.DisputeId);

        dbContext.Notifications.Add(entry);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Queued dispute resolved notification for dispute {DisputeId} (payment {PaymentId}, user {UserId}, outcome {Resolution})",
            evt.DisputeId, evt.PaymentId, evt.UserId, evt.Resolution);
    }
}
