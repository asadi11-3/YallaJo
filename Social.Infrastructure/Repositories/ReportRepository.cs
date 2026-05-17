using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Social.Infrastructure.Repositories;

internal sealed class ReportRepository(SocialDbContext context) : EfRepository<Report, Guid>(context), IReportRepository
{
    public async Task<IReadOnlyList<Report>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => await context.Reports.AsNoTracking()
            .Where(r => r.EntityType == entityType && r.EntityId == entityId)
            .ToListAsync(ct).ConfigureAwait(false);

    public Task<int> CountByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => context.Reports.CountAsync(r => r.EntityType == entityType && r.EntityId == entityId, ct);

    public async Task<IReadOnlyList<Report>> GetPendingAsync(CancellationToken ct = default)
        => await context.Reports.AsNoTracking()
            .Where(r => r.Status == ReportStatus.Pending)
            .ToListAsync(ct).ConfigureAwait(false);
}
