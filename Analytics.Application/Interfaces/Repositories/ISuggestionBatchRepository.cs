using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface ISuggestionBatchRepository : IRepository<SuggestionBatch, Guid>
{
    Task<IReadOnlyList<SuggestionBatch>> GetBySourceAsync(EntityType sourceKind, Guid sourceId, CancellationToken ct = default);
    Task<SuggestionBatch?> GetBySourceAsync(EntityType sourceKind, Guid sourceId, SuggestionContext context, CancellationToken ct = default);
    Task<IReadOnlyList<SuggestionBatch>> GetStaleAsync(int batchSize, CancellationToken ct = default);
    Task<IReadOnlyList<SuggestionBatch>> GetAllAsync(CancellationToken ct = default);
}
