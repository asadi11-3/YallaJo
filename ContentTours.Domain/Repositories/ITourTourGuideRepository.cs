using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContentTours.Domain.Entities;

namespace ContentTours.Domain.Repositories;

public interface ITourTourGuideRepository
{
    Task<List<TourTourGuide>> GetByTourIdAsync(Guid tourId, CancellationToken cancellationToken = default);
    Task<TourTourGuide?> GetAsync(Guid tourId, Guid guideUserId, CancellationToken cancellationToken = default);
    void Add(TourTourGuide guide);
    void Remove(TourTourGuide guide);
}
