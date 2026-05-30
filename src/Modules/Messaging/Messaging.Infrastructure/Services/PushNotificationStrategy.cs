using Messaging.Application.Interfaces;
using Messaging.Contracts.Services;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;

namespace Messaging.Infrastructure.Services;

/// <summary>Push notification strategy — stub. Returns Success=false (Phase 2).</summary>
internal sealed class PushNotificationStrategy : INotificationChannelStrategy
{
    public NotificationChannel Channel => NotificationChannel.Push;

    public Task<ChannelDispatchResult> DeliverAsync(Notification notification, CancellationToken ct = default)
        => Task.FromResult(new ChannelDispatchResult(
            Success: false,
            ExternalRef: null,
            FailureReason: "Push not implemented in this sprint (Phase 2)"));
}
