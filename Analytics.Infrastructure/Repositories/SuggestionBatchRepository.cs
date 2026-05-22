using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class SuggestionBatchRepository(AnalyticsDbContext context) : EfRepository<SuggestionBatch, Guid>(context), ISuggestionBatchRepository
{
    public async Task<IReadOnlyList<SuggestionBatch>> GetBySourceAsync(EntityType sourceKind, Guid sourceId, CancellationToken ct = default)
        => await context.SuggestionBatches
            .Where(x => x.SourceKind == sourceKind && x.SourceId == sourceId)
            .ToListAsync(ct);

    public Task<SuggestionBatch?> GetBySourceAsync(EntityType sourceKind, Guid sourceId, SuggestionContext contextValue, CancellationToken ct = default)
        => context.SuggestionBatches.FirstOrDefaultAsync(x => x.SourceKind == sourceKind && x.SourceId == sourceId && x.Context == contextValue, ct);

    public async Task<IReadOnlyList<SuggestionBatch>> GetStaleAsync(int batchSize, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-6);
        return await context.SuggestionBatches
            .Where(x => x.IsStale || x.ComputedAt < cutoff)
            .OrderByDescending(x => x.IsStale)
            .ThenBy(x => x.ComputedAt)
            .Take(Math.Clamp(batchSize, 1, 1000))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SuggestionBatch>> GetAllAsync(CancellationToken ct = default)
        => await context.SuggestionBatches.AsNoTracking().ToListAsync(ct);
}
