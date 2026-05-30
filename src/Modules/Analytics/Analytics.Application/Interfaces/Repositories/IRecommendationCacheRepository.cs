using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface IRecommendationCacheRepository : IRepository<RecommendationCache, Guid>
{
    Task<IReadOnlyList<RecommendationCache>> GetByBatchIdAsync(Guid batchId, CancellationToken ct = default);
    Task<IReadOnlyList<RecommendationCache>> GetByUserAsync(Guid userId, int limit, CancellationToken ct = default);
    Task DeleteByBatchIdAsync(Guid batchId, CancellationToken ct = default);
}
