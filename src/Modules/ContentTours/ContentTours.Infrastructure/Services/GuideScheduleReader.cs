using ContentTours.Contracts.Tours;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentTours.Infrastructure.Services;

internal sealed class GuideScheduleReader(ContentToursDbContext dbContext) : IGuideScheduleReader
{
    public async Task<IReadOnlyList<GuideScheduleSummary>> GetActiveSchedulesAsync(CancellationToken ct = default)
    {
        return await dbContext.GuideSchedules
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new GuideScheduleSummary(
                s.Id,
                s.GuideTourOfferingId,
                s.TourGuideId,
                s.TourId,
                s.DayOfWeek,
                s.StartTime,
                s.EndTime))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
