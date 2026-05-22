using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class EntityAttributeSnapshotRepository(AnalyticsDbContext context) : EfRepository<EntityAttributeSnapshot, Guid>(context), IEntityAttributeSnapshotRepository
{
    public Task<EntityAttributeSnapshot?> GetByEntityAsync(EntityType entityKind, Guid entityId, CancellationToken ct = default)
        => context.EntityAttributeSnapshots.FirstOrDefaultAsync(x => x.EntityKind == entityKind && x.EntityId == entityId, ct);

    public async Task<IReadOnlyDictionary<(EntityType Kind, Guid Id), EntityAttributeSnapshot>> GetByEntitiesAsync(
        IEnumerable<(EntityType Kind, Guid Id)> keys, CancellationToken ct = default)
    {
        var keyList = keys.ToList();
        if (keyList.Count == 0)
            return new Dictionary<(EntityType, Guid), EntityAttributeSnapshot>();

        // Group by entity kind to build efficient OR queries
        var entityIds = keyList.Select(k => k.Id).Distinct().ToList();
        var entityKinds = keyList.Select(k => k.Kind).Distinct().ToList();

        var snapshots = await context.EntityAttributeSnapshots
            .AsNoTracking()
            .Where(x => entityKinds.Contains(x.EntityKind) && entityIds.Contains(x.EntityId))
            .ToListAsync(ct);

        return snapshots.ToDictionary(s => (s.EntityKind, s.EntityId));
    }

    public async Task<IReadOnlyList<EntityAttributeSnapshot>> GetActiveByKindAsync(EntityType entityKind, CancellationToken ct = default)
        => await context.EntityAttributeSnapshots
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.EntityKind == entityKind)
            .Where(x => x.Status == "Published" || x.Status == "Approved")
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EntityAttributeSnapshot>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default)
    {
        var token = categoryId.ToString("D");
        return await context.EntityAttributeSnapshots
            .AsNoTracking()
            .Where(x => x.CategoryIdsJson != null && x.CategoryIdsJson.Contains(token))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EntityAttributeSnapshot>> GetNearbyAsync(decimal latitude, decimal longitude, double radiusKm, EntityType? kindFilter, int limit, CancellationToken ct = default)
    {
        var radius = Math.Max(radiusKm, 0d);
        var latDelta = (decimal)(radius / 111d);
        var lngDivisor = Math.Max(Math.Cos((double)latitude * Math.PI / 180d), 0.01d);
        var lngDelta = (decimal)(radius / (111d * lngDivisor));
        var minLat = latitude - latDelta;
        var maxLat = latitude + latDelta;
        var minLng = longitude - lngDelta;
        var maxLng = longitude + lngDelta;

        var query = context.EntityAttributeSnapshots.AsNoTracking()
            .Where(x => x.LocationLatitude != null && x.LocationLongitude != null)
            .Where(x => x.LocationLatitude >= minLat && x.LocationLatitude <= maxLat && x.LocationLongitude >= minLng && x.LocationLongitude <= maxLng);

        if (kindFilter is not null)
            query = query.Where(x => x.EntityKind == kindFilter);

        return await query
            .OrderBy(x => Math.Abs((double)(x.LocationLatitude!.Value - latitude)) + Math.Abs((double)(x.LocationLongitude!.Value - longitude)))
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);
    }
}
