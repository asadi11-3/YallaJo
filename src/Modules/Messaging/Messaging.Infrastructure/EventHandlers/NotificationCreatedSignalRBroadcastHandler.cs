using MediatR;
using Messaging.Domain.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class NotificationCreatedSignalRBroadcastHandler(
    IServiceProvider serviceProvider,
    ILogger<NotificationCreatedSignalRBroadcastHandler> logger)
    : INotificationHandler<DomainEventNotification<NotificationCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<NotificationCreatedDomainEvent> notification,
        CancellationToken ct)
    {
        var hubContext = serviceProvider.GetService<IHubContext<Hub>>();
        if (hubContext is null)
        {
            logger.LogDebug("SignalR hub context is not registered; skipping notification broadcast.");
            return;
        }

        var evt = notification.Event;
        await hubContext.Clients.Group($"user:{evt.UserId}").SendAsync(
            "ReceiveNotification",
            new { evt.NotificationId, evt.Type, evt.Channel },
            ct);
    }
}
