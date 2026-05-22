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
/// Notifies a creator when their post has been featured.
/// InApp: always. Email: opt-in (default off).
/// </summary>
public sealed class CreatorPostFeaturedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<CreatorPostFeaturedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorPostFeaturedIntegrationEvent>>
{
    private const string Title = "Your Post Has Been Featured!";

    public async Task Handle(
        IntegrationEventNotification<CreatorPostFeaturedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (CreatorPostFeatured {PostId}) already processed; skipping.",
                notification.MessageId, notification.Event.PostId);
            return;
        }

        var evt = notification.Event;
        var untilNote = evt.FeaturedUntilUtc.HasValue
            ? $" until {evt.FeaturedUntilUtc.Value:yyyy-MM-dd}"
            : "";
        var body = $"Congratulations! Your creator post has been featured on YallaJo{untilNote}. It will receive extra visibility across the platform.";

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
            "Messaging: notifications queued for CreatorPostFeatured PostId={PostId}",
            evt.PostId);
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
