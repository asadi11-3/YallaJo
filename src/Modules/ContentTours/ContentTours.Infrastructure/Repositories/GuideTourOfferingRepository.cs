using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentTours.Infrastructure.Repositories;

internal sealed class GuideTourOfferingRepository(ContentToursDbContext context)
    : EfEntityRepository<GuideTourOffering, Guid>(context), IGuideTourOfferingRepository
{
    public Task<GuideTourOffering?> GetByTourAndGuideAsync(Guid tourId, Guid tourGuideId, CancellationToken ct = default, bool asNoTracking = true)
    {
        var query = context.GuideTourOfferings.Where(o => o.TourId == tourId && o.TourGuideId == tourGuideId);
        if (asNoTracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(ct);
    }

    public Task<int> CountActiveOfferingsAsync(Guid tourId, CancellationToken ct = default) =>
        context.GuideTourOfferings.CountAsync(o => o.TourId == tourId && o.Status == Domain.Enums.GuideOfferingStatus.Active, ct);

    public async Task<IReadOnlyList<GuideTourOffering>> GetByTourIdAsync(Guid tourId, CancellationToken ct = default, bool asNoTracking = true)
    {
        var query = context.GuideTourOfferings.Where(o => o.TourId == tourId);
        if (asNoTracking) query = query.AsNoTracking();
        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GuideTourOffering>> GetByGuideIdAsync(Guid tourGuideId, CancellationToken ct = default, bool asNoTracking = true)
    {
        var query = context.GuideTourOfferings.Where(o => o.TourGuideId == tourGuideId);
        if (asNoTracking) query = query.AsNoTracking();
        return await query.ToListAsync(ct);
    }
}
