using ContentBlogs.Contracts.IntegrationEvents;
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
/// Notifies a creator/author when their blog has been removed by an admin.
/// InApp: always. Email: opt-in (default off).
/// </summary>
public sealed class CreatorPostRemovedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<CreatorPostRemovedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<BlogRemovedIntegrationEvent>>
{
    private const string Title = "Blog Removed";
    private const string Body  = "Your blog has been removed by a moderator. If you believe this was in error, please contact support.";

    public async Task Handle(
        IntegrationEventNotification<BlogRemovedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (BlogRemoved {BlogId}) already processed; skipping.",
                notification.MessageId, notification.Event.BlogId);
            return;
        }

        var evt = notification.Event;

        // ── InApp — always ──────────────────────────────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.AuthorId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.High,
            title:      Title,
            body:       Body,
            entityType: "Blog",
            entityId:   evt.BlogId));

        // ── Email — opt-in (default off) ────────────────────────────────────
        if (await IsChannelEnabledAsync(evt.AuthorId, NotificationType.Business, NotificationChannel.Email, false, ct))
        {
            dbContext.Notifications.Add(Notification.Create(
                userId:     evt.AuthorId,
                type:       NotificationType.Business,
                channel:    NotificationChannel.Email,
                priority:   NotificationPriority.High,
                title:      Title,
                body:       Body,
                entityType: "Blog",
                entityId:   evt.BlogId));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: notifications queued for BlogRemoved BlogId={BlogId}",
            evt.BlogId);
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
