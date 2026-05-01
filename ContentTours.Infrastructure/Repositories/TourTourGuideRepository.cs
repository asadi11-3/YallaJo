using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ContentTours.Infrastructure.Repositories;

internal sealed class TourTourGuideRepository : ITourTourGuideRepository
{
    private readonly ContentToursDbContext _dbContext;

    public TourTourGuideRepository(ContentToursDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<TourTourGuide>> GetByTourIdAsync(Guid tourId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<TourTourGuide>()
            .Where(g => g.TourId == tourId)
            .ToListAsync(cancellationToken);
    }

    public Task<TourTourGuide?> GetAsync(Guid tourId, Guid guideUserId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<TourTourGuide>()
            .FirstOrDefaultAsync(g => g.TourId == tourId && g.TourGuideId == guideUserId, cancellationToken);
    }

    public void Add(TourTourGuide guide) => _dbContext.Set<TourTourGuide>().Add(guide);

    public void Remove(TourTourGuide guide) => _dbContext.Set<TourTourGuide>().Remove(guide);
}
