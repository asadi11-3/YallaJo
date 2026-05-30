using Analytics.Domain.Entities;

namespace Analytics.Application.Interfaces.Repositories;

public interface ISuggestionMetricRepository
{
    Task AddAsync(SuggestionMetric entity, CancellationToken ct = default);
    Task<IReadOnlyList<SuggestionMetric>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
