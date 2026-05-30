using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;

namespace Social.Infrastructure.Repositories;

internal sealed class ContentModerationLogRepository(SocialDbContext context)
    : IContentModerationLogRepository
{
    public async Task AddAsync(ContentModerationLog entry, CancellationToken ct = default)
    {
        await context.ContentModerationLogs.AddAsync(entry, ct).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<ContentModerationLog> Items, Guid? NextCursor)> GetPageAsync(
        Guid? afterId, int pageSize, CancellationToken ct = default)
    {
        var size = Math.Clamp(pageSize, 1, 50);
        var query = context.ContentModerationLogs.AsNoTracking();

        if (afterId.HasValue)
            query = query.Where(l => l.Id.CompareTo(afterId.Value) < 0);

        var items = await query
            .OrderByDescending(l => l.Id)
            .Take(size + 1)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        Guid? nextCursor = null;
        if (items.Count > size)
        {
            nextCursor = items[size].Id;
            items = items.Take(size).ToList();
        }

        return (items, nextCursor);
    }

    public async Task<IReadOnlyList<ContentModerationLog>> GetByEntityAsync(
        ReportableEntityType entityType, Guid entityId, CancellationToken ct = default)
        => await context.ContentModerationLogs
            .AsNoTracking()
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderByDescending(l => l.ActionedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
