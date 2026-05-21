using Messaging.Application.Interfaces;
using Messaging.Contracts.Services;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Messaging.Infrastructure.Services;

/// <summary>Delivers notification via SignalR to user:{userId} group. M-R6.
/// IHubContext is resolved lazily so this strategy can register before the Hub is mapped.</summary>
internal sealed class InAppNotificationStrategy(
    IServiceProvider serviceProvider,
    ILogger<InAppNotificationStrategy> logger)
    : INotificationChannelStrategy
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public async Task<ChannelDispatchResult> DeliverAsync(Notification notification, CancellationToken ct = default)
    {
        // Resolve lazily — hub may not be registered in test/design-time contexts
        var hubContext = serviceProvider.GetService(typeof(IHubContext<Hub>)) as IHubContext<Hub>;
        if (hubContext is null)
        {
            logger.LogDebug("SignalR IHubContext<Hub> not available; InApp delivery skipped for {Id}", notification.Id);
            return new ChannelDispatchResult(Success: true, ExternalRef: null, FailureReason: null);
        }

        try
        {
            await hubContext.Clients
                .Group($"user:{notification.UserId}")
                .SendAsync("ReceiveNotification", new
                {
                    notification.Id,
                    notification.Type,
                    notification.Title,
                    notification.Body,
                    notification.Priority,
                    notification.CreatedAt,
                }, ct);

            return new ChannelDispatchResult(Success: true, ExternalRef: null, FailureReason: null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "InApp delivery failed for notification {Id}", notification.Id);
            return new ChannelDispatchResult(Success: false, ExternalRef: null, FailureReason: ex.Message);
        }
    }
}
