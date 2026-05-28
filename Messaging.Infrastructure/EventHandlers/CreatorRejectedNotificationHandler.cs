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
/// Notifies a creator when their application has been rejected.
/// Includes rejection reason and 14-day cooling period info.
/// InApp: always. Email: opt-in (default off).
/// </summary>
public sealed class CreatorRejectedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<CreatorRejectedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorApplicationRejectedIntegrationEvent>>
{
    private const string Title = "Creator Application Update";

    public async Task Handle(
        IntegrationEventNotification<CreatorApplicationRejectedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (CreatorApplicationRejected {ApplicationId}) already processed; skipping.",
                notification.MessageId, notification.Event.ApplicationId);
            return;
        }

        var evt = notification.Event;
        var body = $"Your creator application was not approved. Reason: {evt.Reason}. You may reapply after 14 days.";

        // ── InApp — always ───────────────────────────────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.ApplicantUserId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.Medium,
            title:      Title,
            body:       body,
            entityType: "CreatorApplication",
            entityId:   evt.ApplicationId));

        // ── Email — opt-in (default off) ─────────────────────────────────────
        if (await IsChannelEnabledAsync(evt.ApplicantUserId, NotificationType.Business, NotificationChannel.Email, false, ct))
        {
            dbContext.Notifications.Add(Notification.Create(
                userId:     evt.ApplicantUserId,
                type:       NotificationType.Business,
                channel:    NotificationChannel.Email,
                priority:   NotificationPriority.Medium,
                title:      Title,
                body:       body,
                entityType: "CreatorApplication",
                entityId:   evt.ApplicationId));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: notifications queued for CreatorApplicationRejected UserId={UserId} ApplicationId={ApplicationId}",
            evt.ApplicantUserId, evt.ApplicationId);
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
