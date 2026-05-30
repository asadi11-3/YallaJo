using Social.Domain.Entities;

namespace Social.Domain.Repositories;

public interface IBusinessSnapshotRepository
{
    Task<BusinessSnapshot?> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default);
    Task UpsertAsync(BusinessSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetDeletedBusinessIdsAsync(int take, CancellationToken ct = default);
}
