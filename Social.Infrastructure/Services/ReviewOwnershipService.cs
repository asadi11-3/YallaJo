using Microsoft.EntityFrameworkCore;
using Social.Contracts.Authorization;
using Social.Domain.Entities;
using Social.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Authorization;

namespace Social.Infrastructure.Services;

/// <summary>
/// Cross-module ownership probe for the Review aggregate. Uses a no-tracking
/// projection that ignores soft-delete query filters so callers can distinguish
/// missing rows from soft-deleted rows. Review is an <c>AuditableEntity</c> and
/// therefore carries an <c>IsDeleted</c> column.
/// </summary>
internal sealed class ReviewOwnershipService(SocialDbContext dbContext) : IReviewOwnershipService
{
    public async Task<EntityOwnershipResolution> GetReviewOwnershipAsync(
        Guid reviewId, CancellationToken ct = default)
    {
        var row = await dbContext.Set<Review>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r => r.Id == reviewId)
            .Select(r => new { r.UserId, r.IsDeleted })
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
