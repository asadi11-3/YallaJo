using ContentTours.Contracts.Authorization;
using ContentTours.Domain.Entities;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Authorization;

namespace ContentTours.Infrastructure.Services;

/// <summary>
/// Cross-module ownership probe for the Tour aggregate. Uses a no-tracking
/// projection that ignores soft-delete query filters so callers can distinguish
/// missing rows from soft-deleted rows.
/// </summary>
internal sealed class TourOwnershipService(ContentToursDbContext dbContext) : ITourOwnershipService
{
    public async Task<EntityOwnershipResolution> GetTourOwnershipAsync(
        Guid tourId, CancellationToken ct = default)
    {
        var row = await dbContext.Set<Tour>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.Id == tourId)
            .Select(t => new { t.CreatedByUserId, t.IsDeleted })
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
}
