// Security.Domain/Repositories/IAuditLogRepository.cs
using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace Security.Domain.Repositories;

public interface IAuditLogRepository
{
    
    Task<PaginatedResult<AuditLog>> GetPagedAsync(
        Guid? userId, int page, int pageSize, CancellationToken ct = default);
}
