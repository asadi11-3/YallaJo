using Messaging.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Messaging.Domain.Repositories;

public interface INotificationRepository : IRepository<Notification, Guid>
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, int take, int skip, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetUnsentAsync(int batchSize, CancellationToken ct = default);
}
