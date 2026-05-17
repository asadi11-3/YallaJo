using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Messaging.Domain.Repositories;

public interface INotificationPreferenceRepository : IRepository<NotificationPreference, Guid>
{
    Task<IReadOnlyList<NotificationPreference>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<NotificationPreference?> GetByUserAndTypeAsync(Guid userId, NotificationType type, NotificationChannel channel, CancellationToken ct = default);
}
