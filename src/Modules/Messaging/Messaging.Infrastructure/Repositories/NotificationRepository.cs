using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Messaging.Infrastructure.Repositories;

internal sealed class NotificationRepository(MessagingDbContext context)
    : EfRepository<Notification, Guid>(context), INotificationRepository
{
    // All queries below go through _context.Notifications, which has a global
    // HasQueryFilter(x => !x.IsDeleted). Soft-deleted notifications are therefore
    // automatically excluded from user lists, unread counts, mark-all-read,
    // cleanup and pending-dispatch. Do NOT add IgnoreQueryFilters() here.
    private readonly MessagingDbContext _context = context;

    public async Task<(IReadOnlyList<Notification> Items, Guid? NextCursor)> GetByUserPagedAsync(
        Guid userId, NotificationType? type, bool? isRead, DateTime? from, DateTime? to,
        Guid? afterId, int pageSize, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = _context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (type.HasValue) query = query.Where(n => n.Type == type.Value);
        if (isRead.HasValue) query = query.Where(n => n.ReadAt.HasValue == isRead.Value);
        if (from.HasValue) query = query.Where(n => n.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(n => n.CreatedAt <= to.Value);
        if (afterId.HasValue) query = query.Where(n => n.Id.CompareTo(afterId.Value) < 0);
        var rows = await query.OrderByDescending(n => n.Id).Take(pageSize + 1).ToListAsync(ct);
        Guid? next = rows.Count > pageSize ? rows[pageSize].Id : null;
        return ((IReadOnlyList<Notification>)rows.Take(pageSize).ToList(), next);
    }

    public Task<int> GetUnreadCountByUserAsync(Guid userId, CancellationToken ct = default)
        => _context.Notifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && n.ReadAt == null, ct);

    public async Task MarkAllAsReadByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _context.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now), ct);
    }

    public async Task<IReadOnlyList<Notification>> GetOldReadForCleanupAsync(
        DateTime olderThan, IReadOnlyList<NotificationType> excludedTypes, int batchSize, CancellationToken ct = default)
        => await _context.Notifications.AsNoTracking()
            .Where(n => n.ReadAt != null && n.CreatedAt < olderThan && !excludedTypes.Contains(n.Type))
            .OrderBy(n => n.CreatedAt).Take(batchSize).ToListAsync(ct);

    public async Task<IReadOnlyList<Notification>> GetPendingDispatchAsync(int batchSize, CancellationToken ct = default)
        => await _context.Notifications.AsNoTracking()
            .Where(n => n.SentAt == null && n.FailureReason == null)
            .OrderBy(n => n.CreatedAt).Take(batchSize).ToListAsync(ct);
}
