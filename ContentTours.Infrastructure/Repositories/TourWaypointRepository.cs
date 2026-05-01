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

internal sealed class TourWaypointRepository : ITourWaypointRepository
{
    private readonly ContentToursDbContext _dbContext;

    public TourWaypointRepository(ContentToursDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<TourWaypoint>> GetByTourIdAsync(Guid tourId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<TourWaypoint>()
            .Where(w => w.TourId == tourId)
            .OrderBy(w => w.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public Task<TourWaypoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<TourWaypoint>().FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public void Add(TourWaypoint waypoint) => _dbContext.Set<TourWaypoint>().Add(waypoint);

    public void Remove(TourWaypoint waypoint) => _dbContext.Set<TourWaypoint>().Remove(waypoint);
}
