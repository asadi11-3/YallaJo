// Security.Domain/Repositories/IAuditLogRepository.cs
using Security.Domain.Entities;

namespace Security.Domain.Repositories;

public interface IAuditLogRepository
{
    /// <summary>
    /// Returns a page of audit log entries, ordered by OccurredAt descending.
    /// Optionally filtered by UserId.
    /// </summary>
    Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        Guid? userId, int page, int pageSize, CancellationToken ct = default);
}
