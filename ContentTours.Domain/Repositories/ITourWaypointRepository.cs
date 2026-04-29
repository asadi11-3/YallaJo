using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContentTours.Domain.Entities;

namespace ContentTours.Domain.Repositories;

public interface ITourWaypointRepository
{
    Task<List<TourWaypoint>> GetByTourIdAsync(Guid tourId, CancellationToken cancellationToken = default);
    Task<TourWaypoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(TourWaypoint waypoint);
    void Remove(TourWaypoint waypoint);
}
