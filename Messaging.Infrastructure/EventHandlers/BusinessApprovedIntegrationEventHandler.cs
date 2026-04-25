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
/// Creates owner notifications when a Business is approved.
///
/// Business rules (see plan §4.5):
/// - InApp notification: always created — owner needs to see this in-app.
/// - Email notification: opt-in only (default = off). Owner must have
///   <c>NotificationPreference(Business, Email, IsEnabled=true)</c> to receive email.
/// - Priority: High — owner has been waiting for approval.
/// - EntityType/EntityId included so the mobile app / web can deep-link to the business.
/// - Rule B: no external side effects (email send, push) here — a separate
///   BackgroundService polls Notifications and dispatches via SMTP/FCM.
/// </summary>
public sealed class BusinessApprovedIntegrationEventHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<BusinessApprovedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<BusinessApprovedIntegrationEvent>>
{
    private const string Title = "Business Approved";
    private const string Body  = "Your business has been approved and is now live on YallaJo. Congratulations!";

    public async Task Handle(
        IntegrationEventNotification<BusinessApprovedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (BusinessApproved {BusinessId}) already processed; skipping.",
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

        // ── Email — opt-in (default off per Rule C) ──────────────────────────
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
            "Messaging: notifications queued for BusinessApproved Owner={OwnerId} Business={BusinessId} EmailOptIn={EmailOptIn}",
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
