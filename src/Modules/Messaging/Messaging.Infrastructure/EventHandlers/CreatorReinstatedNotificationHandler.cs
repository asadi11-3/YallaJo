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
/// Notifies a creator when their profile has been reinstated by an admin.
/// InApp: always. Email: opt-in (default off).
/// </summary>
public sealed class CreatorReinstatedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<CreatorReinstatedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorProfileReinstatedIntegrationEvent>>
{
    private const string Title = "Creator Profile Reinstated";
    private const string Body  = "Your creator profile has been reinstated. You can now continue publishing articles on YallaJo.";

    public async Task Handle(
        IntegrationEventNotification<CreatorProfileReinstatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (CreatorProfileReinstated {ProfileId}) already processed; skipping.",
                notification.MessageId, notification.Event.ProfileId);
            return;
        }

        var evt = notification.Event;

        // ── InApp — always ───────────────────────────────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.UserId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.Medium,
            title:      Title,
            body:       Body,
            entityType: "CreatorProfile",
            entityId:   evt.ProfileId));

        // ── Email — opt-in (default off) ─────────────────────────────────────
        if (await IsChannelEnabledAsync(evt.UserId, NotificationType.Business, NotificationChannel.Email, false, ct))
        {
            dbContext.Notifications.Add(Notification.Create(
                userId:     evt.UserId,
                type:       NotificationType.Business,
                channel:    NotificationChannel.Email,
                priority:   NotificationPriority.Medium,
                title:      Title,
                body:       Body,
                entityType: "CreatorProfile",
                entityId:   evt.ProfileId));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: notifications queued for CreatorProfileReinstated UserId={UserId} ProfileId={ProfileId}",
            evt.UserId, evt.ProfileId);
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
