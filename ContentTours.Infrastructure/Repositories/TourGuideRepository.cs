using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
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

    /// <summary>
    /// Loads the TourGuide aggregate by owning user identity, including Languages and
    /// Specializations. F15 fix backing <c>GetTourGuideByUserIdQueryHandler</c>.
    /// </summary>
    public Task<TourGuide?> GetWithDetailsByUserIdAsync(
        Guid userId,
        CancellationToken ct = default,
        bool asNoTracking = true)
    {
        var query = context.TourGuides
            .Include(g => g.Languages)
            .Include(g => g.Specializations)
            .Where(g => g.UserId == userId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(ct);
    }

    // SMELL #2 removed: the misleading GetBySlugAsync(Guid id) overload that actually
    // filtered by aggregate Id was deleted. Callers that need lookup by id already use
    // GetWithDetailsAsync(Guid). The remaining overload is the real slug lookup.

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

    public async Task<(IReadOnlyList<TourGuide> Items, int TotalCount)> ListActiveAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        // SMELL #1 fix: filter by Status == Active. Previously relied only on the
        // soft-delete query filter, so Suspended guides could leak into public lists.
        var query = context.TourGuides
            .Include(g => g.Languages)
            .Include(g => g.Specializations)
            .Where(g => g.Status == TourGuideStatus.Active)
            .AsNoTracking()
            .OrderByDescending(g => g.AverageRating)
            .ThenByDescending(g => g.CompletedTourCount);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
