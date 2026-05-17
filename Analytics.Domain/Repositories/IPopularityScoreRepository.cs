using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Domain.Repositories;

public interface IPopularityScoreRepository : IRepository<PopularityScore, Guid>
{
    Task<PopularityScore?> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
    Task<IReadOnlyList<PopularityScore>> GetTrendingAsync(string entityType, int take, CancellationToken ct = default);
}
