using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentTours.Infrastructure.Repositories;

internal sealed class GuidePricingTierRepository(ContentToursDbContext context)
    : EfEntityRepository<GuidePricingTier, Guid>(context), IGuidePricingTierRepository
{
    public async Task<IReadOnlyList<GuidePricingTier>> GetByOfferingIdAsync(Guid offeringId, CancellationToken ct = default, bool asNoTracking = true)
    {
        var query = context.GuidePricingTiers.Where(t => t.GuideTourOfferingId == offeringId);
        if (asNoTracking) query = query.AsNoTracking();
        return await query.OrderBy(t => t.MinParticipants).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GuidePricingTier>> GetByTourAndGuideAsync(Guid tourId, Guid tourGuideId, CancellationToken ct = default, bool asNoTracking = true)
    {
        var query = context.GuidePricingTiers.Where(t => t.TourId == tourId && t.TourGuideId == tourGuideId);
        if (asNoTracking) query = query.AsNoTracking();
        return await query.OrderBy(t => t.MinParticipants).ToListAsync(ct);
    }
}
