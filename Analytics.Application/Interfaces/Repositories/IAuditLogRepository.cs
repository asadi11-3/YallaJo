using Analytics.Domain.Entities;
using Analytics.Domain.Enums;

namespace Analytics.Application.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry, CancellationToken ct = default);
    Task<AuditLog?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<(IReadOnlyList<AuditLog> Items, long? NextId)> GetPageAsync(string? entityType, Guid? entityId, Guid? userId, AuditLogAction? action, DateTime? from, DateTime? to, long? afterId, int pageSize, CancellationToken ct = default);
    Task<long> CountAsync(DateTime from, DateTime to, CancellationToken ct = default);
    IAsyncEnumerable<AuditLog> StreamAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
