using Messaging.Domain.Entities;
using Messaging.Domain.Enums;

namespace Messaging.Domain.Repositories;

/// <summary>Repository for NotificationDeliveryAttempt queries (write goes through Notification aggregate).</summary>
public interface INotificationDeliveryAttemptRepository
{
    Task AddAsync(NotificationDeliveryAttempt attempt, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationDeliveryAttempt>> GetPendingForRetryAsync(
        NotificationChannel channel, DateTime dueBy, int batchSize, CancellationToken ct = default);
    Task<int> CountAttemptsAsync(Guid notificationId, NotificationChannel channel, CancellationToken ct = default);
}
