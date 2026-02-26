using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;
using Accounts.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.EventHandlers;

/// <summary>
/// Reacts to a user being created in the Security module by creating
/// a Profile in the Accounts module using the FirstName and LastName
/// provided at registration time.
/// </summary>
public sealed class UserCreatedIntegrationEventHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    IAccountsInboxStore inboxStore,
    ILogger<UserCreatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserCreatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<UserCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        // Inbox check — idempotency guard: skip if already processed (retry/duplicate)
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "Accounts: Message {MessageId} (UserCreated for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var evt = notification.Event;

        // Guard: profile may already exist (defensive, beyond inbox idempotency)
        if (await profileRepository.ExistsByUserIdAsync(evt.UserId, ct))
        {
            logger.LogWarning(
                "Accounts: Profile already exists for user {UserId} — marking inbox and skipping.",
                evt.UserId);

            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var profile = Profile.Create(evt.UserId, evt.FirstName, evt.LastName);

        await profileRepository.AddAsync(profile, ct);

        // Record in inbox and persist atomically with the profile
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Accounts: Profile {ProfileId} created for user {UserId} ({FirstName} {LastName}).",
            profile.Id, evt.UserId, evt.FirstName, evt.LastName);
    }
}