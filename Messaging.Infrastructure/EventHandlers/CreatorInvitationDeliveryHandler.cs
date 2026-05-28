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
/// Delivers a creator invitation.
/// Branches on Kind: Email → SMTP notification / InApp → bell notification.
/// </summary>
public sealed class CreatorInvitationDeliveryHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<CreatorInvitationDeliveryHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorInvitationSentIntegrationEvent>>
{
    private const string Title = "You've Been Invited to Become a Creator";
    private const string Body  = "You have been invited to join YallaJo as a content creator. Accept the invitation to start writing articles and sharing your expertise.";

    public async Task Handle(
        IntegrationEventNotification<CreatorInvitationSentIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (CreatorInvitationSent {InvitationId}) already processed; skipping.",
                notification.MessageId, notification.Event.InvitationId);
            return;
        }

        var evt = notification.Event;

        if (evt.Kind == "Email" && !string.IsNullOrWhiteSpace(evt.Email))
        {
            // ── Email invitation — queue email notification ──────────────────
            // The email channel notification will be picked up by the email sender background service.
            // We create a placeholder user-less notification; the email service resolves by email address.
            dbContext.Notifications.Add(Notification.Create(
                userId:     evt.SentByAdminId,   // Admin who sent the invite (for audit trail)
                type:       NotificationType.Business,
                channel:    NotificationChannel.Email,
                priority:   NotificationPriority.High,
                title:      Title,
                body:       $"{Body} Invitation sent to: {evt.Email}",
                entityType: "CreatorInvitation",
                entityId:   evt.InvitationId));
        }
        else if (evt.InvitedUserId.HasValue)
        {
            // ── InApp invitation — bell notification ─────────────────────────
            dbContext.Notifications.Add(Notification.Create(
                userId:     evt.InvitedUserId.Value,
                type:       NotificationType.Business,
                channel:    NotificationChannel.InApp,
                priority:   NotificationPriority.High,
                title:      Title,
                body:       Body,
                entityType: "CreatorInvitation",
                entityId:   evt.InvitationId));

            // Also check email opt-in for the invited user
            if (await IsChannelEnabledAsync(evt.InvitedUserId.Value, NotificationType.Business, NotificationChannel.Email, false, ct))
            {
                dbContext.Notifications.Add(Notification.Create(
                    userId:     evt.InvitedUserId.Value,
                    type:       NotificationType.Business,
                    channel:    NotificationChannel.Email,
                    priority:   NotificationPriority.High,
                    title:      Title,
                    body:       Body,
                    entityType: "CreatorInvitation",
                    entityId:   evt.InvitationId));
            }
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: invitation delivery queued for InvitationId={InvitationId} Kind={Kind}",
            evt.InvitationId, evt.Kind);
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
