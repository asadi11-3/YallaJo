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
/// Notifies a creator when their post has been rejected by an admin.
/// InApp: always. Email: opt-in (default off).
/// </summary>
public sealed class CreatorPostRejectedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<CreatorPostRejectedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorPostRejectedIntegrationEvent>>
{
    private const string Title = "Creator Post Rejected";

    public async Task Handle(
        IntegrationEventNotification<CreatorPostRejectedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (CreatorPostRejected {PostId}) already processed; skipping.",
                notification.MessageId, notification.Event.PostId);
            return;
        }

        var evt = notification.Event;
        var body = $"Your creator post has been rejected. Reason: {evt.Reason}. You may revise and resubmit.";

        // ── InApp — always ──────────────────────────────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.CreatorProfileId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.High,
            title:      Title,
            body:       body,
            entityType: "CreatorPost",
            entityId:   evt.PostId));

        // ── Email — opt-in (default off) ────────────────────────────────────
        if (await IsChannelEnabledAsync(evt.CreatorProfileId, NotificationType.Business, NotificationChannel.Email, false, ct))
        {
            dbContext.Notifications.Add(Notification.Create(
                userId:     evt.CreatorProfileId,
                type:       NotificationType.Business,
                channel:    NotificationChannel.Email,
                priority:   NotificationPriority.High,
                title:      Title,
                body:       body,
                entityType: "CreatorPost",
                entityId:   evt.PostId));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: notifications queued for CreatorPostRejected PostId={PostId} Reason={Reason}",
            evt.PostId, evt.Reason);
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
