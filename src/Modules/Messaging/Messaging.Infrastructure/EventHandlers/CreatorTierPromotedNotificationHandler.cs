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
/// Notifies a creator when their trust tier has been promoted.
/// InApp: always. Email: opt-in (default off).
/// </summary>
public sealed class CreatorTierPromotedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<CreatorTierPromotedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorTierPromotedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<CreatorTierPromotedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (CreatorTierPromoted {ProfileId}) already processed; skipping.",
                notification.MessageId, notification.Event.CreatorProfileId);
            return;
        }

        var evt = notification.Event;
        var title = $"Trust Tier Promoted to {evt.NewTier}!";
        var body = $"Congratulations! Your creator trust tier has been promoted from {evt.PreviousTier} to {evt.NewTier}. This unlocks new privileges on the platform.";

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

        // ── Email — opt-in (default off) ────────────────────────────────────
        if (await IsChannelEnabledAsync(evt.UserId, NotificationType.Business, NotificationChannel.Email, false, ct))
        {
            dbContext.Notifications.Add(Notification.Create(
                userId:     evt.UserId,
                type:       NotificationType.Business,
                channel:    NotificationChannel.Email,
                priority:   NotificationPriority.High,
                title:      title,
                body:       body,
                entityType: "CreatorProfile",
                entityId:   evt.CreatorProfileId));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: notifications queued for CreatorTierPromoted UserId={UserId} {PreviousTier} -> {NewTier}",
            evt.UserId, evt.PreviousTier, evt.NewTier);
    }

    private async Task<bool> IsChannelEnabledAsync(
        Guid userId, NotificationType type, NotificationChannel channel, bool defaultEnabled, CancellationToken ct)
    {
        var pref = await dbContext.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.UserId == userId && p.NotificationType == type && p.Channel == channel, ct);
        return pref?.IsEnabled ?? defaultEnabled;
    }
}
