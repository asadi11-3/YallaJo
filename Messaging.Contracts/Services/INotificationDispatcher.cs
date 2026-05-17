namespace Messaging.Contracts.Services;

public interface INotificationDispatcher
{
    Task DispatchAsync(Guid notificationId, CancellationToken ct = default);
}
