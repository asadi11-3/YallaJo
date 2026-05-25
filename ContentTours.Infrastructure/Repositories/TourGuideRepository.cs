using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentTours.Infrastructure.Repositories;

internal sealed class TourGuideRepository(ContentToursDbContext context)
    : EfRepository<TourGuide, Guid>(context), ITourGuideRepository
{
    public Task<TourGuide?> GetWithDetailsAsync(
        Guid id,
        CancellationToken ct = default,
        bool asNoTracking = true)
    {
        var query = context.TourGuides
            .Include(guide => guide.Languages)
            .Include(guide => guide.Specializations)
            .Where(guide => guide.Id == id);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(ct);
    }

    public Task<TourGuide?> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default,
        bool asNoTracking = true)
    {
        var query = context.TourGuides.Where(guide => guide.UserId == userId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(ct);
    }

    public Task<int> CountAssignedToursAsync(Guid guideUserId, CancellationToken ct = default) =>
        context.GuideTourOfferings
            .Where(o => o.TourGuideId == guideUserId)
            .Select(o => o.TourId)
            .Distinct()
            .CountAsync(ct);

    public Task<TourGuide?> GetBySlugAsync(
        Guid id,
        CancellationToken ct = default,
        bool asNoTracking = true)
    {
        var query = context.TourGuides.Where(g => g.Id == id);
        if (asNoTracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(ct);
    }

    public Task<TourGuide?> GetBySlugAsync(
        string slug,
        CancellationToken ct = default,
        bool asNoTracking = true)
    {
        var query = context.TourGuides.Where(g => g.Slug == slug);
        if (asNoTracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(ct);
    }

    public Task<bool> IsSlugTakenAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = context.TourGuides.Where(g => g.Slug == slug);
        if (excludeId.HasValue) query = query.Where(g => g.Id != excludeId.Value);
        return query.AnyAsync(ct);
    }
}
