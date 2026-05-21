using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;

namespace Social.Infrastructure.Repositories;

internal sealed class TourSnapshotRepository(SocialDbContext context) : ITourSnapshotRepository
{
    public Task<TourSnapshot?> GetByTourIdAsync(Guid tourId, CancellationToken ct = default)
        => context.TourSnapshots.FirstOrDefaultAsync(x => x.TourId == tourId, ct);

    public async Task UpsertAsync(TourSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await context.TourSnapshots
            .FirstOrDefaultAsync(x => x.TourId == snapshot.TourId, ct)
            .ConfigureAwait(false);

        if (existing is null)
            await context.TourSnapshots.AddAsync(snapshot, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetDeletedTourIdsAsync(int take, CancellationToken ct = default)
        => await context.TourSnapshots
            .AsNoTracking()
            .Where(x => x.IsDeleted)
            .OrderBy(x => x.DeletedAt)
            .Select(x => x.TourId)
            .Take(take)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
