using Accounts.Application.Interfaces;
using Accounts.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.EventHandlers;

/// <summary>
/// Consumes EmailVerifiedIntegrationEvent from the Security outbox.
/// No Accounts-specific action at this time — ensures the outbox message is drained.
/// </summary>
public sealed class EmailVerifiedIntegrationEventHandler(
    IAccountsInboxStore inboxStore,
    IAccountsUnitOfWork unitOfWork,
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
                "Accounts: Message {MessageId} (EmailVerified for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Accounts: EmailVerified acknowledged for user {UserId} ({Email}).",
            notification.Event.UserId, notification.Event.EmailAddress);
    }
}
