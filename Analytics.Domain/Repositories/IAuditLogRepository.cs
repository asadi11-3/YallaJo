using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Domain.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog, long>
{
    Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid userId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default);
}
