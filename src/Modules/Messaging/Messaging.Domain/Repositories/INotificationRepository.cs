using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Messaging.Domain.Repositories;

/// <summary>Repository for Notification aggregate queries.</summary>
public interface INotificationRepository : IRepository<Notification, Guid>
{
    /// <summary>Paginated list of notifications for a user (cursor = Id).</summary>
    Task<(IReadOnlyList<Notification> Items, Guid? NextCursor)> GetByUserPagedAsync(
        Guid userId,
        NotificationType? type,
        bool? isRead,
        DateTime? from,
        DateTime? to,
        Guid? afterId,
        int pageSize,
        CancellationToken ct = default);

    Task<int> GetUnreadCountByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Batch mark all unread as read for a user.</summary>
    Task MarkAllAsReadByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns read notifications older than cutoff, excluding critical types.</summary>
    Task<IReadOnlyList<Notification>> GetOldReadForCleanupAsync(
        DateTime olderThan,
        IReadOnlyList<NotificationType> excludedTypes,
        int batchSize,
        CancellationToken ct = default);

    /// <summary>Returns notifications with pending delivery attempts.</summary>
    Task<IReadOnlyList<Notification>> GetPendingDispatchAsync(int batchSize, CancellationToken ct = default);
}
