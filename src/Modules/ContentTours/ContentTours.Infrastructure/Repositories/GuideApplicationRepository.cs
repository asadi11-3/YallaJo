using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentTours.Infrastructure.Repositories;

internal sealed class GuideApplicationRepository(ContentToursDbContext context)
    : EfRepository<GuideApplication, Guid>(context), IGuideApplicationRepository
{
    public Task<GuideApplication?> GetWithDetailsAsync(Guid id, CancellationToken ct = default, bool asNoTracking = true)
    {
        var query = context.GuideApplications.Where(a => a.Id == id);
        if (asNoTracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(ct);
    }

    public Task<bool> HasPendingApplicationAsync(Guid tourId, Guid tourGuideId, CancellationToken ct = default) =>
        context.GuideApplications.AnyAsync(
            a => a.TourId == tourId && a.TourGuideId == tourGuideId &&
                 (a.Status == GuideApplicationStatus.Draft || a.Status == GuideApplicationStatus.Submitted), ct);

    public Task<int> CountApplicationsAsync(Guid tourGuideId, GuideApplicationStatus? statusFilter = null, CancellationToken ct = default)
    {
        var query = context.GuideApplications.Where(a => a.TourGuideId == tourGuideId);
        if (statusFilter.HasValue) query = query.Where(a => a.Status == statusFilter.Value);
        return query.CountAsync(ct);
    }

    public async Task<IReadOnlyList<GuideApplication>> GetByTourIdAsync(Guid tourId, GuideApplicationStatus? statusFilter = null, CancellationToken ct = default)
    {
        var query = context.GuideApplications.Where(a => a.TourId == tourId).AsNoTracking();
        if (statusFilter.HasValue) query = query.Where(a => a.Status == statusFilter.Value);
        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GuideApplication>> GetByGuideIdAsync(Guid tourGuideId, GuideApplicationStatus? statusFilter = null, CancellationToken ct = default)
    {
        var query = context.GuideApplications.Where(a => a.TourGuideId == tourGuideId).AsNoTracking();
        if (statusFilter.HasValue) query = query.Where(a => a.Status == statusFilter.Value);
        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
    }
}
