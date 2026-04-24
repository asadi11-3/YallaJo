
using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace Security.Domain.Repositories;

public interface IAuditLogRepository
{
    /// <summary>
    /// Phase 4 — paginated audit query with optional filters. Every
    /// filter is composed conjunctively; nulls are ignored. Results
    /// are ordered by <c>OccurredAt</c> descending.
    /// </summary>
    Task<PaginatedResult<AuditLog>> GetPagedAsync(
        Guid? userId,
        int page,
        int pageSize,
        Guid? actorUserId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);
}
