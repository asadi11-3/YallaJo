using ContentPlaces.Contracts.Places;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentPlaces.Infrastructure.Services;

internal sealed class PlaceExistenceService(ContentPlacesDbContext dbContext) : IPlaceExistenceService
{
    public async Task<PlaceExistenceStatus> GetStatusAsync(Guid? placeId, CancellationToken ct = default)
    {
        if (!placeId.HasValue)
            return PlaceExistenceStatus.NotChecked;

        // IgnoreQueryFilters so we can see soft-deleted rows; AsNoTracking because this is
        // a read-only existence probe.
        var row = await dbContext.Set<Place>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.Id == placeId.Value)
            .Select(p => new { p.IsDeleted })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (row is null)
            return PlaceExistenceStatus.NotFound;

        return row.IsDeleted
            ? PlaceExistenceStatus.Deleted
            : PlaceExistenceStatus.Active;
    }
}
