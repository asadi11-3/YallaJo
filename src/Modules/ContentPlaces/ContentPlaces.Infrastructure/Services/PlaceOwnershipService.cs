using ContentPlaces.Contracts.Places;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Authorization;

namespace ContentPlaces.Infrastructure.Services;

/// <summary>
/// Cross-module ownership probe for Place and Business aggregates.
/// Both probes use a no-tracking projection that ignores soft-delete query
/// filters so callers can distinguish missing rows from soft-deleted rows.
/// </summary>
internal sealed class PlaceOwnershipService(ContentPlacesDbContext dbContext) : IPlaceOwnershipService
{
    public async Task<EntityOwnershipResolution> GetPlaceOwnershipAsync(
        Guid placeId, CancellationToken ct = default)
    {
        var row = await dbContext.Set<Place>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.Id == placeId)
            .Select(p => new { p.CreatedByUserId, p.IsDeleted })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (row is null)
        {
            return new EntityOwnershipResolution(
                IsSupported: true,
                Exists: false,
                IsDeleted: false,
                OwnerUserId: null);
        }

        return new EntityOwnershipResolution(
            IsSupported: true,
            Exists: true,
            IsDeleted: row.IsDeleted,
            OwnerUserId: row.CreatedByUserId);
    }

    public async Task<EntityOwnershipResolution> GetBusinessOwnershipAsync(
        Guid businessId, CancellationToken ct = default)
    {
        var row = await dbContext.Set<Business>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.Id == businessId)
            .Select(b => new { b.OwnerId, b.IsDeleted })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (row is null)
        {
            return new EntityOwnershipResolution(
                IsSupported: true,
                Exists: false,
                IsDeleted: false,
                OwnerUserId: null);
        }

        return new EntityOwnershipResolution(
            IsSupported: true,
            Exists: true,
            IsDeleted: row.IsDeleted,
            OwnerUserId: row.OwnerId);
    }
}
