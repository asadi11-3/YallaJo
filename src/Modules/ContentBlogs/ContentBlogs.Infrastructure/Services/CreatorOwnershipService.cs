using ContentBlogs.Contracts.Authorization;
using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Authorization;

namespace ContentBlogs.Infrastructure.Services;

/// <summary>
/// Cross-module ownership probe for the CreatorProfile aggregate. Uses a no-tracking
/// projection that ignores soft-delete query filters so callers can distinguish
/// missing rows from soft-deleted rows.
/// </summary>
internal sealed class CreatorOwnershipService(ContentBlogsDbContext dbContext) : ICreatorOwnershipService
{
    public async Task<EntityOwnershipResolution> GetCreatorProfileOwnershipAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var row = await dbContext.Set<CreatorProfile>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.Id == profileId)
            .Select(p => new { p.UserId, p.IsDeleted })
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
            OwnerUserId: row.UserId);
    }
}
