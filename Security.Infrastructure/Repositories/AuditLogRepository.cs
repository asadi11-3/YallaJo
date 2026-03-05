// Security.Infrastructure/Repositories/AuditLogRepository.cs
using Microsoft.EntityFrameworkCore;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace Security.Infrastructure.Repositories;

internal sealed class AuditLogRepository(SecurityDbContext context) : IAuditLogRepository
{
    public async Task<PaginatedResult<AuditLog>> GetPagedAsync(
        Guid? userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.AuditLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PaginatedResult<AuditLog>(items, total, page, pageSize);
    }
}
