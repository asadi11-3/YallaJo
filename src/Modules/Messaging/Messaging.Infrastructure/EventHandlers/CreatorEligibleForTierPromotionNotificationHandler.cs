using ContentBlogs.Contracts.IntegrationEvents.Creators;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>
/// Notifies admins when a creator becomes eligible for a tier promotion.
/// InApp: always (system notification for admin review).
/// </summary>
public sealed class CreatorEligibleForTierPromotionNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<CreatorEligibleForTierPromotionNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorEligibleForTierPromotionIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<CreatorEligibleForTierPromotionIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (CreatorEligibleForTierPromotion {ProfileId}) already processed; skipping.",
                notification.MessageId, notification.Event.CreatorProfileId);
            return;
        }

        var evt = notification.Event;
        var title = "Creator Eligible for Tier Promotion";
        var body = $"A creator (Tier {evt.CurrentTier}) is now eligible for promotion to {evt.EligibleForTier}. Please review in the admin queue.";

        // NOTE: Admin notification — routed to moderators with AdminCreatorQueue permissions.
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.CreatorProfileId, // Placeholder — admin routing resolves actual recipients
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.Medium,
            title:      title,
            body:       body,
            entityType: "CreatorProfile",
            entityId:   evt.CreatorProfileId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: admin notification queued for CreatorEligibleForTierPromotion ProfileId={ProfileId} CurrentTier={CurrentTier} EligibleFor={EligibleFor}",
            evt.CreatorProfileId, evt.CurrentTier, evt.EligibleForTier);
    }
}
