using Messaging.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Messaging.Domain.Repositories;

public interface INotificationTemplateRepository : IRepository<NotificationTemplate, Guid>
{
    Task<NotificationTemplate?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationTemplate>> GetActiveAsync(CancellationToken ct = default);
}
