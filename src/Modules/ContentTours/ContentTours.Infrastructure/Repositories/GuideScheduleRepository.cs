using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentTours.Infrastructure.Repositories;

internal sealed class GuideScheduleRepository(ContentToursDbContext context)
    : EfEntityRepository<GuideSchedule, Guid>(context), IGuideScheduleRepository
{
    public async Task<IReadOnlyList<GuideSchedule>> GetByOfferingIdAsync(Guid offeringId, CancellationToken ct = default, bool asNoTracking = true)
    {
        var query = context.GuideSchedules.Where(s => s.GuideTourOfferingId == offeringId);
        if (asNoTracking) query = query.AsNoTracking();
        return await query.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GuideSchedule>> GetByTourAndGuideAsync(Guid tourId, Guid tourGuideId, CancellationToken ct = default, bool asNoTracking = true)
    {
        var query = context.GuideSchedules.Where(s => s.TourId == tourId && s.TourGuideId == tourGuideId);
        if (asNoTracking) query = query.AsNoTracking();
        return await query.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).ToListAsync(ct);
    }
}
