using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Messaging.Infrastructure.Repositories;

internal sealed class NotificationDeliveryAttemptRepository(MessagingDbContext context)
    : INotificationDeliveryAttemptRepository
{
    public async Task AddAsync(NotificationDeliveryAttempt attempt, CancellationToken ct = default)
    {
        context.NotificationDeliveryAttempts.Add(attempt);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<NotificationDeliveryAttempt>> GetPendingForRetryAsync(
        NotificationChannel channel, DateTime dueBy, int batchSize, CancellationToken ct = default)
        => await context.NotificationDeliveryAttempts.AsNoTracking()
            .Where(a => a.Channel == channel
                && a.Status == NotificationDeliveryStatus.Pending
                && a.AttemptedAt <= dueBy)
            .OrderBy(a => a.AttemptedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task<int> CountAttemptsAsync(Guid notificationId, NotificationChannel channel, CancellationToken ct = default)
        => context.NotificationDeliveryAttempts.AsNoTracking()
            .CountAsync(a => a.NotificationId == notificationId && a.Channel == channel, ct);
}
