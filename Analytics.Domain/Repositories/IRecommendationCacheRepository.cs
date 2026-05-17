using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Domain.Repositories;

public interface IRecommendationCacheRepository : IRepository<RecommendationCache, Guid>
{
    Task<IReadOnlyList<RecommendationCache>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<RecommendationCache>> GetExpiredAsync(DateTime threshold, CancellationToken ct = default);
}
