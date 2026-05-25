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
/// Notifies a creator when their post has been published.
/// InApp: always. Email: opt-in (default off).
/// </summary>
public sealed class CreatorPostPublishedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<CreatorPostPublishedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<BlogPublishedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<BlogPublishedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (BlogPublished {BlogId}) already processed; skipping.",
                notification.MessageId, notification.Event.BlogId);
            return;
        }

        var evt = notification.Event;
        var title = "Your Blog Is Live!";
        var body = $"Your blog \"{evt.Title}\" has been published. It is now visible to the community.";

        // ── InApp — always ──────────────────────────────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.AuthorId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.Medium,
            title:      title,
            body:       body,
            entityType: "Blog",
            entityId:   evt.BlogId));

        // ── Email — opt-in (default off) ────────────────────────────────────
        if (await IsChannelEnabledAsync(evt.AuthorId, NotificationType.Business, NotificationChannel.Email, false, ct))
        {
            dbContext.Notifications.Add(Notification.Create(
                userId:     evt.AuthorId,
                type:       NotificationType.Business,
                channel:    NotificationChannel.Email,
                priority:   NotificationPriority.Medium,
                title:      title,
                body:       body,
                entityType: "Blog",
                entityId:   evt.BlogId));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: notifications queued for BlogPublished BlogId={BlogId}",
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
