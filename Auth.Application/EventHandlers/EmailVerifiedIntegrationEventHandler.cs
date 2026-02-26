using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.EventHandlers;

/// <summary>
/// Reacts to email verification in the Security module.
/// Currently logs the event for auditing. Device/Session/RefreshToken creation
/// is handled directly in the VerifyEmailCommandHandler.
/// </summary>
public sealed class EmailVerifiedIntegrationEventHandler(
    IAuthInboxStore inboxStore,
    IAuthUnitOfWork unitOfWork,
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

        // Mark as processed — MUST persist the inbox record to prevent infinite reprocessing
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
