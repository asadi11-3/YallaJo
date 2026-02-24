using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.EventHandlers;

/// <summary>
/// Reacts to a user being created in the Security module by bootstrapping a
/// trusted Device record in the Auth module.
///
/// Lives in Application (use-case layer) because it orchestrates domain operations.
/// Infrastructure concerns (EF, transport) are handled by OutboxProcessor and EfInboxStore.
/// </summary>
public sealed class UserCreatedIntegrationEventHandler(
    IDeviceRepository deviceRepository,
    IAuthUnitOfWork unitOfWork,
    IInboxStore inboxStore,
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
                "Auth: Message {MessageId} (UserCreated for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var evt = notification.Event;
        var bootstrapToken = $"bootstrap:{evt.UserId}";

        var existing = await deviceRepository.FirstOrDefaultAsync(
            d => d.UserId == evt.UserId && d.DeviceToken == bootstrapToken,
            ct: ct);

        if (existing is null)
        {
            var bootstrapDevice = Device.Create(
                evt.UserId,
                bootstrapToken,
                "system/bootstrap",
                "Bootstrap Device");

            await deviceRepository.AddAsync(bootstrapDevice, ct);

            logger.LogInformation("Auth: Bootstrap device created for user {UserId}.", evt.UserId);
        }
        else
        {
            logger.LogInformation("Auth: Bootstrap device already exists for user {UserId}.", evt.UserId);
        }

        // Record in inbox and persist atomically with the business change
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
