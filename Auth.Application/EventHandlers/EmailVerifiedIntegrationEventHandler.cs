using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.EventHandlers;
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
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
