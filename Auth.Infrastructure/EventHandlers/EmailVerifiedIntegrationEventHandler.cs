using Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Infrastructure.EventHandlers;

public sealed class EmailVerifiedIntegrationEventHandler(
    IDeviceRepository deviceRepository,
    IAuthUnitOfWork unitOfWork,
    ILogger<EmailVerifiedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<EmailVerifiedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<EmailVerifiedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;
        var bootstrapToken = $"bootstrap:{evt.UserId}";

        var bootstrapDevice = await deviceRepository.FirstOrDefaultAsync(
            d => d.UserId == evt.UserId && d.DeviceToken == bootstrapToken,
            asNoTracking: false,
            ct: ct);

        if (bootstrapDevice is null)
        {
            logger.LogInformation("No bootstrap device found for user {UserId}; skipping verification transition", evt.UserId);
            return;
        }

        if (!bootstrapDevice.IsTrusted)
        {
            bootstrapDevice.Trust();
            deviceRepository.Update(bootstrapDevice);
            await unitOfWork.SaveChangesAsync(ct);
        }

        logger.LogInformation("Auth login bootstrap is trusted for user {UserId}", evt.UserId);
    }
}
