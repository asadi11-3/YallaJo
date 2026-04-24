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
        Guid? userId,
        int page,
        int pageSize,
        Guid? actorUserId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var query = context.AuditLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId);

        if (actorUserId.HasValue)
            query = query.Where(a => a.ActorUserId == actorUserId);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (from.HasValue)
            query = query.Where(a => a.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.OccurredAt <= to.Value);

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
