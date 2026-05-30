using Social.Domain.Entities;

namespace Social.Domain.Repositories;

public interface ITourSnapshotRepository
{
    Task<TourSnapshot?> GetByTourIdAsync(Guid tourId, CancellationToken ct = default);
    Task UpsertAsync(TourSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetDeletedTourIdsAsync(int take, CancellationToken ct = default);
}
