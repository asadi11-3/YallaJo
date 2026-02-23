using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Infrastructure.EventHandlers;

public sealed class UserCreatedIntegrationEventHandler(
    AuthDbContext dbContext,
    ILogger<UserCreatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserCreatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<UserCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;
        var bootstrapToken = $"bootstrap:{evt.UserId}";

        var existing = await dbContext.Devices
            .FirstOrDefaultAsync(d => d.UserId == evt.UserId && d.DeviceToken == bootstrapToken, ct);

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

        dbContext.Devices.Add(bootstrapDevice);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Auth bootstrap device created for user {UserId}", evt.UserId);
    }
}
