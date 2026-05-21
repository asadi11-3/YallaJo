using Auth.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>
/// Sends a WelcomeEmail (InApp + Email) notification when a new user registers.
/// Critical = false; Email opt-in defaults to true for welcome message.
/// </summary>
public sealed class AuthUserRegisteredHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<AuthUserRegisteredHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserRegisteredIntegrationEvent>>
{
    private const string Title = "Welcome to YallaJo!";
    private const string Body  = "Your account has been created. Start exploring amazing tours!";

    public async Task Handle(
        IntegrationEventNotification<UserRegisteredIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "Messaging: Message {MessageId} (UserRegistered {UserId}) already processed; skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var evt = notification.Event;

        // InApp — always
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.UserId,
            type:       NotificationType.WelcomeEmail,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.Medium,
            title:      Title,
            body:       Body));

        // Email — default on for welcome message
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.UserId,
            type:       NotificationType.WelcomeEmail,
            channel:    NotificationChannel.Email,
            priority:   NotificationPriority.Medium,
            title:      Title,
            body:       Body));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: WelcomeEmail notifications created for UserId={UserId}",
            evt.UserId);
    }
}
