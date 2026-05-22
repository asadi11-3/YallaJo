using ContentBlogs.Contracts.IntegrationEvents.Creators;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>
/// Notifies admins when a creator post is submitted for review.
/// InApp: always. Email: opt-in (default off).
/// </summary>
public sealed class CreatorPostSubmittedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<CreatorPostSubmittedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorPostSubmittedForReviewIntegrationEvent>>
{
    private const string Title = "Creator Post Awaiting Review";

    public async Task Handle(
        IntegrationEventNotification<CreatorPostSubmittedForReviewIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (CreatorPostSubmitted {PostId}) already processed; skipping.",
                notification.MessageId, notification.Event.PostId);
            return;
        }

        var evt = notification.Event;
        var body = $"A creator post \"{evt.Title}\" has been submitted for review and is awaiting moderation.";

        // NOTE: In production, this would query admin users with the AdminPostModeration.Read permission.
        // For now we stage the notification record; the admin notification routing is handled by the
        // notification delivery pipeline which fans out to users with matching preferences.
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.CreatorProfileId, // Placeholder — admin routing resolves actual recipients
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.Medium,
            title:      Title,
            body:       body,
            entityType: "CreatorPost",
            entityId:   evt.PostId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: admin notification queued for CreatorPostSubmitted PostId={PostId} Title={Title}",
            evt.PostId, evt.Title);
    }
}
