using Accounts.Application.Interfaces;
using Accounts.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.EventHandlers;

/// <summary>
/// Consumes PhoneNumberUpdatedIntegrationEvent from the Security outbox.
/// Prevents orphaned outbox messages — no Accounts-specific action required at this time.
/// </summary>
public sealed class PhoneNumberUpdatedIntegrationEventHandler(
    IAccountsInboxStore inboxStore,
    IAccountsUnitOfWork unitOfWork,
    ILogger<PhoneNumberUpdatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PhoneNumberUpdatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<PhoneNumberUpdatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "Accounts: Message {MessageId} (PhoneNumberUpdated for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Accounts: PhoneNumberUpdated acknowledged for user {UserId}.",
            notification.Event.UserId);
    }
}
