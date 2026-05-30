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
/// Creates owner notifications when a Business application is rejected.
///
/// Business rules (see plan §4.6):
/// - InApp notification: always created.
/// - Email notification: UNCONDITIONAL (overrides user preference).
///   Reason: owner may not check the app for days and needs the rejection reason
///   + remediation steps urgently. Revenue-blocking — same treatment as Suspended.
/// - Priority: High.
/// - Rejection reason is included in the body so the owner knows what to fix.
/// </summary>
public sealed class BusinessRejectedIntegrationEventHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<BusinessRejectedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<BusinessRejectedIntegrationEvent>>
{
    private const string Title = "Business Application Rejected";

    public async Task Handle(
        IntegrationEventNotification<BusinessRejectedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (BusinessRejected {BusinessId}) already processed; skipping.",
                notification.MessageId, notification.Event.BusinessId);
            return;
        }

        var evt  = notification.Event;
        var body = string.IsNullOrWhiteSpace(evt.Reason)
            ? "Your business application was not approved. Please contact support for details."
            : $"Your business application was not approved. Reason: {evt.Reason}";

        // ── InApp — always ───────────────────────────────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.OwnerId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.High,
            title:      Title,
            body:       body,
            entityType: "Business",
            entityId:   evt.BusinessId));

        // ── Email — unconditional (preference override — see plan Rule C) ─────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.OwnerId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.Email,
            priority:   NotificationPriority.High,
            title:      Title,
            body:       body,
            entityType: "Business",
            entityId:   evt.BusinessId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: notifications queued for BusinessRejected Owner={OwnerId} Business={BusinessId}",
            evt.OwnerId, evt.BusinessId);
    }
}
