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
/// Notifies a provider user when additional documents are requested for their application.
///
/// Listens to <see cref="ProviderStatusChangedIntegrationEvent"/> and filters for
/// <c>NewStatus == "MoreDocsNeeded"</c> transitions, since more-docs-requested does not
/// emit a dedicated integration event — it uses the shared status-changed event.
///
/// - InApp: always created.
/// - Email: opt-in only (default = off).
/// </summary>
public sealed class ProviderMoreDocsRequestedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<ProviderMoreDocsRequestedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ProviderStatusChangedIntegrationEvent>>
{
    private const string Title = "Additional Documents Required";
    private const string Body  = "Your provider application requires additional documents. Please log in to upload the missing documents and resubmit your application.";

    public async Task Handle(
        IntegrationEventNotification<ProviderStatusChangedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        // Only handle MoreDocsNeeded transitions — other status changes are handled elsewhere.
        if (!string.Equals(evt.NewStatus, "MoreDocsNeeded", StringComparison.Ordinal))
        {
            return;
        }

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (ProviderMoreDocsNeeded {ApplicationId}) already processed; skipping.",
                notification.MessageId, evt.ApplicationId);
            return;
        }

        // ── InApp — always ───────────────────────────────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.UserId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.High,
            title:      Title,
            body:       Body,
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
                body:       Body,
                entityType: "ProviderApplication",
                entityId:   evt.ApplicationId));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: notifications queued for ProviderMoreDocsNeeded UserId={UserId} ApplicationId={ApplicationId} EmailOptIn={EmailOptIn}",
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
