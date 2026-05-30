using Accounts.Contracts.IntegrationEvents;
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
/// Notifies a provider user when their application has been rejected.
/// - InApp: always created.
/// - Email: opt-in only (default = off).
/// </summary>
public sealed class ProviderRejectedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<ProviderRejectedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ProviderRejectedIntegrationEvent>>
{
    private const string Title = "Application Rejected";

    public async Task Handle(
        IntegrationEventNotification<ProviderRejectedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (ProviderRejected {ApplicationId}) already processed; skipping.",
                notification.MessageId, notification.Event.ApplicationId);
            return;
        }

        var evt = notification.Event;
        var body = string.IsNullOrWhiteSpace(evt.Reason)
            ? "Your provider application has been rejected. You may reapply after the cooling period."
            : $"Your provider application has been rejected. Reason: {evt.Reason}. You may reapply after the cooling period.";

        // ── InApp — always ───────────────────────────────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.UserId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.High,
            title:      Title,
            body:       body,
            entityType: "ProviderApplication",
            entityId:   evt.ApplicationId));

        // ── Email — opt-in (default off) ─────────────────────────────────────
        var emailOptIn = await IsChannelEnabledAsync(
            evt.UserId, NotificationType.Business, NotificationChannel.Email,
            defaultEnabled: false, ct);

        if (emailOptIn)
        {
            dbContext.Notifications.Add(Notification.Create(
                userId:     evt.UserId,
                type:       NotificationType.Business,
                channel:    NotificationChannel.Email,
                priority:   NotificationPriority.High,
                title:      Title,
                body:       body,
                entityType: "ProviderApplication",
                entityId:   evt.ApplicationId));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: notifications queued for ProviderRejected UserId={UserId} ApplicationId={ApplicationId} EmailOptIn={EmailOptIn}",
            evt.UserId, evt.ApplicationId, emailOptIn);
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
