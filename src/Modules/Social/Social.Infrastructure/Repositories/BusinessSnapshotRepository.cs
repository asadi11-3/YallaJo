using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;

namespace Social.Infrastructure.Repositories;

internal sealed class BusinessSnapshotRepository(SocialDbContext context) : IBusinessSnapshotRepository
{
    public Task<BusinessSnapshot?> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default)
        => context.BusinessSnapshots.FirstOrDefaultAsync(x => x.BusinessId == businessId, ct);

    public async Task UpsertAsync(BusinessSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await context.BusinessSnapshots
            .FirstOrDefaultAsync(x => x.BusinessId == snapshot.BusinessId, ct)
            .ConfigureAwait(false);

        if (existing is null)
            await context.BusinessSnapshots.AddAsync(snapshot, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetDeletedBusinessIdsAsync(int take, CancellationToken ct = default)
        => await context.BusinessSnapshots
            .AsNoTracking()
            .Where(x => x.IsDeleted)
            .OrderBy(x => x.DeletedAt)
            .Select(x => x.BusinessId)
            .Take(take)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
