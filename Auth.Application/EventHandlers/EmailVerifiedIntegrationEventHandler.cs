using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.EventHandlers;

/// <summary>
/// Reacts to email verification in the Security module.
/// Currently logs the event for auditing. Device/Session/RefreshToken creation
/// is handled directly in the VerifyEmailCommandHandler.
/// </summary>
public sealed class EmailVerifiedIntegrationEventHandler(
    IInboxStore inboxStore,
    ILogger<EmailVerifiedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<EmailVerifiedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<EmailVerifiedIntegrationEvent> notification,
        CancellationToken ct)
    {
        // Inbox check — idempotency guard
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "Auth: Message {MessageId} (EmailVerified for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var evt = notification.Event;

        logger.LogInformation(
            "Auth: EmailVerified event received for user {UserId}, email {Email}.",
            evt.UserId, evt.EmailAddress);

        // Mark as processed (no business operation needed — VerifyEmailCommandHandler
        // already created Device + Session + RefreshToken before this event fires)
        inboxStore.MarkAsProcessed(notification.MessageId);

        // Note: No SaveChanges needed since there are no entity changes.
        // The inbox store tracks in-memory; it will be saved when the next UoW commits.
        await Task.CompletedTask;
    }
}
