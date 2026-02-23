using Auth.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Infrastructure.EventHandlers;

public sealed class EmailVerifiedIntegrationEventHandler(
    AuthDbContext dbContext,
    ILogger<EmailVerifiedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<EmailVerifiedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<EmailVerifiedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;
        var bootstrapToken = $"bootstrap:{evt.UserId}";

        var bootstrapDevice = await dbContext.Devices
            .FirstOrDefaultAsync(d => d.UserId == evt.UserId && d.DeviceToken == bootstrapToken, ct);

        if (bootstrapDevice is null)
        {
            logger.LogInformation("No bootstrap device found for user {UserId}; skipping verification transition", evt.UserId);
            return;
        }

        if (!bootstrapDevice.IsTrusted)
        {
            bootstrapDevice.Trust();
            await dbContext.SaveChangesAsync(ct);
        }

        logger.LogInformation("Auth login bootstrap is trusted for user {UserId}", evt.UserId);
    }
}
