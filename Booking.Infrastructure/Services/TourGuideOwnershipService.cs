using Booking.Contracts.Authorization;
using Booking.Domain.Entities;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Authorization;

namespace Booking.Infrastructure.Services;

/// <summary>
/// Cross-module ownership probe for the TourGuide aggregate. Uses a no-tracking
/// projection that ignores soft-delete query filters so callers can distinguish
/// missing rows from soft-deleted rows.
/// <para>
/// TourGuide carries both <c>IsDeleted</c> (from <c>AuditableEntity</c>) and an
/// independent <c>IsActive</c> flag. The probe maps either condition to
/// <see cref="EntityOwnershipResolution.IsDeleted"/> = <c>true</c>: a soft-deleted
/// guide and an inactive guide are equally non-actionable for cross-module
/// authorization purposes.
/// </para>
/// </summary>
internal sealed class TourGuideOwnershipService(BookingDbContext dbContext) : ITourGuideOwnershipService
{
    public async Task<EntityOwnershipResolution> GetTourGuideOwnershipAsync(
        Guid tourGuideId, CancellationToken ct = default)
    {
        var row = await dbContext.Set<TourGuide>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(g => g.Id == tourGuideId)
            .Select(g => new { g.UserId, g.IsDeleted, g.IsActive })
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

        // Map IsActive=false to IsDeleted=true so handlers see a single "not actionable"
        // signal regardless of whether the guide row was soft-deleted or merely deactivated.
        var notActionable = row.IsDeleted || !row.IsActive;

        return new EntityOwnershipResolution(
            IsSupported: true,
            Exists: true,
            IsDeleted: notActionable,
            OwnerUserId: row.UserId);
    }
}
