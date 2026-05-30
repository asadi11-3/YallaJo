using Social.Domain.Entities;

namespace Social.Domain.Repositories;

public interface IPlaceSnapshotRepository
{
    Task<PlaceSnapshot?> GetByPlaceIdAsync(Guid placeId, CancellationToken ct = default);
    Task UpsertAsync(PlaceSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetDeletedPlaceIdsAsync(int take, CancellationToken ct = default);
}
