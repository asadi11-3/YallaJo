using Analytics.Domain.Entities;

namespace Analytics.Application.Interfaces.Repositories;

public interface IDashboardCacheRepository
{
    Task<DashboardCache?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task UpsertAsync(DashboardCache cache, CancellationToken ct = default);
    Task DeleteExpiredAsync(DateTime now, CancellationToken ct = default);
}
