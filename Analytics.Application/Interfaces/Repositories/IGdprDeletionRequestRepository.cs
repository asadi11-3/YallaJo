using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface IGdprDeletionRequestRepository : IRepository<GdprDeletionRequest, Guid>
{
    Task<GdprDeletionRequest?> GetPendingByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<GdprDeletionRequest>> GetReadyForExecutionAsync(DateTime now, CancellationToken ct = default);
}
