using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Messaging.Domain.Repositories;

/// <summary>Repository for NotificationPreference aggregate queries.</summary>
public interface INotificationPreferenceRepository : IRepository<NotificationPreference, Guid>
{
    Task<IReadOnlyList<NotificationPreference>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(NotificationPreference preference, CancellationToken ct = default);
    Task<bool> IsEnabledForUserAsync(Guid userId, NotificationType type, NotificationChannel channel, CancellationToken ct = default);
    Task<NotificationPreference?> GetByUserAndTypeChannelAsync(Guid userId, NotificationType type, NotificationChannel channel, CancellationToken ct = default);
}
