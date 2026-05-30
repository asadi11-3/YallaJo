using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Infrastructure.Repositories;

internal sealed class AuditLogRepository(AnalyticsDbContext context) : IAuditLogRepository
{
    public Task AddAsync(AuditLog entry, CancellationToken ct = default) => context.AuditLogs.AddAsync(entry, ct).AsTask();
    public Task<AuditLog?> GetByIdAsync(long id, CancellationToken ct = default) => context.AuditLogs.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<(IReadOnlyList<AuditLog> Items, long? NextId)> GetPageAsync(string? entityType, Guid? entityId, Guid? userId, AuditLogAction? action, DateTime? from, DateTime? to, long? afterId, int pageSize, CancellationToken ct = default)
    {
        var size = Math.Clamp(pageSize, 1, 100);
        var q = context.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(x => x.EntityType == entityType);
        if (entityId is not null) q = q.Where(x => x.EntityId == entityId);
        if (userId is not null) q = q.Where(x => x.UserId == userId);
        if (action is not null) q = q.Where(x => x.Action == action);
        if (from is not null) q = q.Where(x => x.OccurredAt >= from);
        if (to is not null) q = q.Where(x => x.OccurredAt <= to);
        if (afterId is not null) q = q.Where(x => x.Id < afterId);
        var items = await q.OrderByDescending(x => x.Id).Take(size + 1).ToListAsync(ct);
        var next = items.Count > size ? items[^1].Id : (long?)null;
        return (items.Take(size).ToList(), next);
    }
    public Task<long> CountAsync(DateTime from, DateTime to, CancellationToken ct = default) => context.AuditLogs.LongCountAsync(x => x.OccurredAt >= from && x.OccurredAt <= to, ct);
    public IAsyncEnumerable<AuditLog> StreamAsync(DateTime from, DateTime to, CancellationToken ct = default) => context.AuditLogs.AsNoTracking().Where(x => x.OccurredAt >= from && x.OccurredAt <= to).OrderBy(x => x.Id).AsAsyncEnumerable();
}
