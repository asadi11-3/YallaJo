using Messaging.Domain.Entities;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Messaging.Infrastructure.Repositories;

internal sealed class NotificationRepository(MessagingDbContext context)
    : EfRepository<Notification, Guid>(context), INotificationRepository
{
    public Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, int take, int skip, CancellationToken ct = default)
        => context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Notification>)t.Result, ct);

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public Task<IReadOnlyList<Notification>> GetUnsentAsync(int batchSize, CancellationToken ct = default)
        => context.Notifications
            .Where(n => n.SentAt == null)
            .Take(batchSize)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Notification>)t.Result, ct);
}
