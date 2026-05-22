using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

public sealed class GdprDeletionRequestRepository(AnalyticsDbContext context)
    : EfRepository<GdprDeletionRequest, Guid>(context), IGdprDeletionRequestRepository
{
    public async Task<GdprDeletionRequest?> GetPendingByUserAsync(Guid userId, CancellationToken ct = default)
        => await context.Set<GdprDeletionRequest>()
            .FirstOrDefaultAsync(r => r.UserId == userId && !r.IsCancelled && !r.IsExecuted, ct);

    public async Task<IReadOnlyList<GdprDeletionRequest>> GetReadyForExecutionAsync(DateTime now, CancellationToken ct = default)
        => await context.Set<GdprDeletionRequest>()
            .Where(r => !r.IsCancelled && !r.IsExecuted && r.ScheduledHardDeleteAt <= now)
            .ToListAsync(ct);
}
