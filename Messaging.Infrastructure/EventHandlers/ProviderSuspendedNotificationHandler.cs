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
/// Notifies a provider user when their account has been suspended.
/// - InApp: always created.
/// - Email: opt-in only (default = off).
/// </summary>
public sealed class ProviderSuspendedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<ProviderSuspendedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ProviderSuspendedIntegrationEvent>>
{
    private const string Title = "Provider Account Suspended";

    public async Task Handle(
        IntegrationEventNotification<ProviderSuspendedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (ProviderSuspended {ApplicationId}) already processed; skipping.",
                notification.MessageId, notification.Event.ApplicationId);
            return;
        }

        var evt = notification.Event;
        var body = string.IsNullOrWhiteSpace(evt.Reason)
            ? "Your provider account has been suspended. Please contact support for more information."
            : $"Your provider account has been suspended. Reason: {evt.Reason}. Please renew your documents and contact support to reinstate.";

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
            "Messaging: notifications queued for ProviderSuspended UserId={UserId} ApplicationId={ApplicationId} EmailOptIn={EmailOptIn}",
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
