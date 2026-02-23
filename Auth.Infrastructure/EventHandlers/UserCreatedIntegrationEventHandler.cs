using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Infrastructure.EventHandlers;

public sealed class UserCreatedIntegrationEventHandler(
    IDeviceRepository deviceRepository,
    IAuthUnitOfWork unitOfWork,
    ILogger<UserCreatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserCreatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<UserCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;
        var bootstrapToken = $"bootstrap:{evt.UserId}";

        var existing = await deviceRepository.FirstOrDefaultAsync(
            d => d.UserId == evt.UserId && d.DeviceToken == bootstrapToken,
            ct: ct);

        if (existing is not null)
        {
            logger.LogInformation("Auth bootstrap already exists for user {UserId}", evt.UserId);
            return;
        }

        var bootstrapDevice = Device.Create(
            evt.UserId,
            bootstrapToken,
            "system/bootstrap",
            "Bootstrap Device");

        await deviceRepository.AddAsync(bootstrapDevice, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Auth bootstrap device created for user {UserId}", evt.UserId);
    }
}
