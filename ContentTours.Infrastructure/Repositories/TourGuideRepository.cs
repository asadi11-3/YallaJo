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
        context.TourTourGuides
            .Where(assignment => assignment.TourGuideId == guideUserId)
            .Select(assignment => assignment.TourId)
            .Distinct()
            .CountAsync(ct);
}
