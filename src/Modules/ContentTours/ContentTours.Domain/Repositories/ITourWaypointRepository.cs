using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContentTours.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

public interface ITourWaypointRepository : IReadRepository<TourWaypoint, Guid>, IWriteRepository<TourWaypoint, Guid>
{
}
