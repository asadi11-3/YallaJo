using Analytics.Domain.Entities;
using Analytics.Domain.Repositories;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class AuditLogRepository(AnalyticsDbContext context)
    : EfRepository<AuditLog, long>(context), IAuditLogRepository
{
    public async Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid userId, int take, CancellationToken ct = default)
        => await context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.OccurredAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default)
        => await context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .ToListAsync(ct);
}
