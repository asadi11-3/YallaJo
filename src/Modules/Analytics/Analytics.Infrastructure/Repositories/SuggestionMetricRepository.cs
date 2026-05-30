using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Infrastructure.Repositories;

public sealed class SuggestionMetricRepository(AnalyticsDbContext context) : ISuggestionMetricRepository
{
    public async Task AddAsync(SuggestionMetric entity, CancellationToken ct = default)
    {
        await context.Set<SuggestionMetric>().AddAsync(entity, ct);
    }

    public async Task<IReadOnlyList<SuggestionMetric>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await context.Set<SuggestionMetric>()
            .Where(m => m.OccurredAt >= from && m.OccurredAt <= to)
            .ToListAsync(ct);
}
