using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Infrastructure.Repositories;

internal sealed class DashboardCacheRepository(AnalyticsDbContext context) : IDashboardCacheRepository
{
    public Task<DashboardCache?> GetByKeyAsync(string key, CancellationToken ct = default) => context.DashboardCaches.FirstOrDefaultAsync(x => x.Key == key, ct);
    public async Task UpsertAsync(DashboardCache cache, CancellationToken ct = default)
    {
        var existing = await context.DashboardCaches.FirstOrDefaultAsync(x => x.Key == cache.Key, ct);
        if (existing is null) await context.DashboardCaches.AddAsync(cache, ct);
        else existing.Update(cache.ValueJson, cache.ExpiresAt, cache.RebuiltAt);
    }
    public async Task DeleteExpiredAsync(DateTime now, CancellationToken ct = default) => await context.DashboardCaches.Where(x => x.ExpiresAt < now).ExecuteDeleteAsync(ct);
}
