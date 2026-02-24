using Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.EventHandlers;

/// <summary>
/// Reacts to email verification in the Security module by marking the
/// bootstrap Device as trusted in the Auth module.
///
/// Lives in Application (use-case layer) because it orchestrates domain operations.
/// </summary>
public sealed class EmailVerifiedIntegrationEventHandler(
    IDeviceRepository deviceRepository,
    IAuthUnitOfWork unitOfWork,
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
        var bootstrapToken = $"bootstrap:{evt.UserId}";

        var bootstrapDevice = await deviceRepository.FirstOrDefaultAsync(
            d => d.UserId == evt.UserId && d.DeviceToken == bootstrapToken,
            asNoTracking: false,
            ct: ct);

        if (bootstrapDevice is null)
        {
            logger.LogWarning(
                "Auth: No bootstrap device found for user {UserId} — skipping verification transition.",
                evt.UserId);
        }
        else if (!bootstrapDevice.IsTrusted)
        {
            bootstrapDevice.Trust();
            deviceRepository.Update(bootstrapDevice);
            logger.LogInformation("Auth: Bootstrap device trusted for user {UserId}.", evt.UserId);
        }
        else
        {
            logger.LogInformation("Auth: Bootstrap device already trusted for user {UserId}.", evt.UserId);
        }

        // Record in inbox and persist atomically
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
