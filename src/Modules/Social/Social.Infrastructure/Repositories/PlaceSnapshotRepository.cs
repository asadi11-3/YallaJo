using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;

namespace Social.Infrastructure.Repositories;

internal sealed class PlaceSnapshotRepository(SocialDbContext context) : IPlaceSnapshotRepository
{
    public Task<PlaceSnapshot?> GetByPlaceIdAsync(Guid placeId, CancellationToken ct = default)
        => context.PlaceSnapshots.FirstOrDefaultAsync(x => x.PlaceId == placeId, ct);

    public async Task UpsertAsync(PlaceSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await context.PlaceSnapshots
            .FirstOrDefaultAsync(x => x.PlaceId == snapshot.PlaceId, ct)
            .ConfigureAwait(false);

        if (existing is null)
            await context.PlaceSnapshots.AddAsync(snapshot, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetDeletedPlaceIdsAsync(int take, CancellationToken ct = default)
        => await context.PlaceSnapshots
            .AsNoTracking()
            .Where(x => x.IsDeleted)
            .OrderBy(x => x.DeletedAt)
            .Select(x => x.PlaceId)
            .Take(take)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
