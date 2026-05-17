using Messaging.Contracts.Services;

namespace Messaging.Infrastructure.Services;

internal sealed class NoopNotificationDispatcher : INotificationDispatcher
{
    public Task DispatchAsync(Guid notificationId, CancellationToken ct = default)
        => Task.CompletedTask;
}
