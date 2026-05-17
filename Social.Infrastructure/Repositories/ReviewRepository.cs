using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Social.Infrastructure.Repositories;

internal sealed class ReviewRepository(SocialDbContext context) : EfRepository<Review, Guid>(context), IReviewRepository
{
    public async Task<IReadOnlyList<Review>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.Reviews.AsNoTracking().Where(r => r.UserId == userId).ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Review>> GetByPlaceIdAsync(Guid placeId, CancellationToken ct = default)
        => await context.Reviews.AsNoTracking().Where(r => r.PlaceId == placeId).ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Review>> GetByTourIdAsync(Guid tourId, CancellationToken ct = default)
        => await context.Reviews.AsNoTracking().Where(r => r.TourId == tourId).ToListAsync(ct).ConfigureAwait(false);

    public Task<bool> ExistsForUserAndTargetAsync(Guid userId, Guid? placeId, Guid? tourId, Guid? tourGuideId, Guid? businessId, CancellationToken ct = default)
        => context.Reviews.AnyAsync(r => r.UserId == userId
            && r.PlaceId == placeId
            && r.TourId == tourId
            && r.TourGuideId == tourGuideId
            && r.BusinessId == businessId, ct);
}
