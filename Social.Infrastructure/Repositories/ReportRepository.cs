using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Social.Infrastructure.Repositories;

internal sealed class ReportRepository(SocialDbContext context)
    : EfRepository<Report, Guid>(context), IReportRepository
{
    public async Task<IReadOnlyList<Report>> GetByEntityAsync(
        ReportableEntityType entityType, Guid entityId, CancellationToken ct = default)
        => await context.Reports
            .AsNoTracking()
            .Where(r => r.EntityType == entityType && r.EntityId == entityId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public Task<int> CountUniqueReportersAsync(
        ReportableEntityType entityType, Guid entityId, CancellationToken ct = default)
        => context.Reports
            .Where(r => r.EntityType == entityType
                        && r.EntityId == entityId
                        && (r.Status == ReportStatus.Open || r.Status == ReportStatus.UnderReview))
            .Select(r => r.ReporterUserId)
            .Distinct()
            .CountAsync(ct);

    public async Task<(IReadOnlyList<Report> Items, Guid? NextCursor)> GetAdminPageAsync(
        Guid? afterId, int pageSize, CancellationToken ct = default)
    {
        var size = Math.Clamp(pageSize, 1, 50);
        var query = context.Reports
            .AsNoTracking()
            .Where(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.UnderReview);

        if (afterId.HasValue)
            query = query.Where(r => r.Id.CompareTo(afterId.Value) < 0);

        var items = await query
            .OrderByDescending(r => r.Id)
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

    public Task<bool> ExistsOpenByReporterAsync(
        Guid reporterUserId, ReportableEntityType entityType, Guid entityId, CancellationToken ct = default)
        => context.Reports
            .AnyAsync(r => r.ReporterUserId == reporterUserId
                           && r.EntityType == entityType
                           && r.EntityId == entityId
                           && (r.Status == ReportStatus.Open || r.Status == ReportStatus.UnderReview), ct);
}
