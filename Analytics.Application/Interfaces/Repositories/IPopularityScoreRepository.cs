using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface IPopularityScoreRepository : IRepository<PopularityScore, Guid>
{
    Task<PopularityScore?> GetByEntityAsync(EntityType entityType, Guid entityId, CancellationToken ct = default);
    Task<IReadOnlyList<PopularityScore>> GetTopByTypeAsync(EntityType entityType, int count, CancellationToken ct = default);
    Task<IReadOnlyList<PopularityScore>> GetTrendingAsync(EntityType entityType, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<PopularityScore>> GetStaleAsync(int batchSize, CancellationToken ct = default);
    Task<IReadOnlyList<PopularityScore>> GetAllByTypeAsync(EntityType entityType, CancellationToken ct = default);
    Task<IReadOnlyList<PopularityScore>> GetAllAsync(CancellationToken ct = default);
}
