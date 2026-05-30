using ContentBlogs.Contracts.IntegrationEvents.Creators;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>
/// Notifies a creator when their trust tier has been demoted.
/// InApp: always. Email: always (critical notification).
/// </summary>
public sealed class CreatorTierDemotedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<CreatorTierDemotedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorTierDemotedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<CreatorTierDemotedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (CreatorTierDemoted {ProfileId}) already processed; skipping.",
                notification.MessageId, notification.Event.CreatorProfileId);
            return;
        }

        var evt = notification.Event;
        var title = $"Trust Tier Changed to {evt.NewTier}";
        var body = $"Your creator trust tier has been changed from {evt.PreviousTier} to {evt.NewTier}. Reason: {evt.Reason}. Some privileges may be restricted.";

        // ── InApp — always ──────────────────────────────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.UserId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.High,
            title:      title,
            body:       body,
            entityType: "CreatorProfile",
            entityId:   evt.CreatorProfileId));

        // ── Email — always (critical notification) ──────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.UserId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.Email,
            priority:   NotificationPriority.High,
            title:      title,
            body:       body,
            entityType: "CreatorProfile",
            entityId:   evt.CreatorProfileId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: notifications queued for CreatorTierDemoted UserId={UserId} {PreviousTier} -> {NewTier}",
            evt.UserId, evt.PreviousTier, evt.NewTier);
    }
}
