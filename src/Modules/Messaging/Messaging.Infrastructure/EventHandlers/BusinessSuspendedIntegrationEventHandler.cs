using ContentPlaces.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>
/// Creates owner notifications when a Business is suspended.
///
/// Business rules (see plan §4.7):
/// - Priority: CRITICAL — business is revenue-blocked; owner must act immediately.
/// - Both InApp AND Email are UNCONDITIONAL (override user preference).
///   A suspended business loses all bookings and public visibility. The owner
///   cannot be allowed to miss this even if they opted out of email.
/// - Body includes the suspension reason and a support contact prompt.
/// - No preference check — Rule C exception for critical events.
/// </summary>
public sealed class BusinessSuspendedIntegrationEventHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<BusinessSuspendedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<BusinessSuspendedIntegrationEvent>>
{
    private const string Title = "Business Suspended";

    public async Task Handle(
        IntegrationEventNotification<BusinessSuspendedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (BusinessSuspended {BusinessId}) already processed; skipping.",
                notification.MessageId, notification.Event.BusinessId);
            return;
        }

        var evt  = notification.Event;
        var body = string.IsNullOrWhiteSpace(evt.Reason)
            ? "Your business has been suspended. Please contact support@yallajo.com for next steps."
            : $"Your business has been suspended. Reason: {evt.Reason} — Contact support@yallajo.com for next steps.";

        // ── InApp — unconditional (Critical) ─────────────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.OwnerId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.Critical,
            title:      Title,
            body:       body,
            entityType: "Business",
            entityId:   evt.BusinessId));

        // ── Email — unconditional (Critical, revenue-blocking) ────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.OwnerId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.Email,
            priority:   NotificationPriority.Critical,
            title:      Title,
            body:       body,
            entityType: "Business",
            entityId:   evt.BusinessId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: Critical notifications queued for BusinessSuspended Owner={OwnerId} Business={BusinessId}",
            evt.OwnerId, evt.BusinessId);
    }
}
