using ContentPlaces.Contracts.IntegrationEvents;
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
/// Creates owner notifications when a suspended Business is reinstated.
///
/// Business rules (see plan §4.8):
/// - InApp notification: always created — owner should see good news immediately.
/// - Email notification: opt-in (default off). The situation is resolved so less
///   urgent than Suspended/Rejected; user preference is respected.
/// - Priority: High.
/// - Body confirms the business is live again.
/// </summary>
public sealed class BusinessReinstatedIntegrationEventHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<BusinessReinstatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<BusinessReinstatedIntegrationEvent>>
{
    private const string Title = "Business Reinstated";
    private const string Body  = "Your business is active again and visible to customers on YallaJo.";

    public async Task Handle(
        IntegrationEventNotification<BusinessReinstatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (BusinessReinstated {BusinessId}) already processed; skipping.",
                notification.MessageId, notification.Event.BusinessId);
            return;
        }

        var evt = notification.Event;

        // ── InApp — always ───────────────────────────────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.OwnerId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.High,
            title:      Title,
            body:       Body,
            entityType: "Business",
            entityId:   evt.BusinessId));

        // ── Email — opt-in (default off) ─────────────────────────────────────
        var emailOptIn = await IsChannelEnabledAsync(
            evt.OwnerId, NotificationType.Business, NotificationChannel.Email,
            defaultEnabled: false, ct);

        if (emailOptIn)
        {
            dbContext.Notifications.Add(Notification.Create(
                userId:     evt.OwnerId,
                type:       NotificationType.Business,
                channel:    NotificationChannel.Email,
                priority:   NotificationPriority.High,
                title:      Title,
                body:       Body,
                entityType: "Business",
                entityId:   evt.BusinessId));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: notifications queued for BusinessReinstated Owner={OwnerId} Business={BusinessId} EmailOptIn={EmailOptIn}",
            evt.OwnerId, evt.BusinessId, emailOptIn);
    }

    private async Task<bool> IsChannelEnabledAsync(
        Guid userId,
        NotificationType type,
        NotificationChannel channel,
        bool defaultEnabled,
        CancellationToken ct)
    {
        var pref = await dbContext.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.UserId == userId
                     && p.NotificationType == type
                     && p.Channel == channel, ct);

        return pref?.IsEnabled ?? defaultEnabled;
    }
}
