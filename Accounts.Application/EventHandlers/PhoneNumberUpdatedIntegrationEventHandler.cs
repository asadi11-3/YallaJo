using Accounts.Application.Caching;
using Accounts.Application.Interfaces;
using Accounts.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.EventHandlers;

public sealed class PhoneNumberUpdatedIntegrationEventHandler(
    IAccountsInboxStore inboxStore,
    IAccountsUnitOfWork unitOfWork,
    HybridCache cache,
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

        // The cached GetProfileResult includes PhoneNumber sourced from the Security module.
        // Evict it so the next read fetches the updated value.
        await cache.RemoveByTagAsync(
            AccountsCacheKeys.UserProfileTag(notification.Event.UserId), ct);

        logger.LogInformation(
            "Accounts: PhoneNumberUpdated processed for user {UserId} — profile cache evicted.",
            notification.Event.UserId);
    }
}
