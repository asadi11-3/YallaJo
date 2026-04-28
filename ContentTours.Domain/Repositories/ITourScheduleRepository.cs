using ContentTours.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

/// <summary>
/// TourSchedule is a non-aggregate child entity (BaseEntity, not IAggregateRoot).
/// Uses IReadRepository + IWriteRepository rather than IRepository which requires IAggregateRoot.
/// </summary>
public interface ITourScheduleRepository : IReadRepository<TourSchedule, Guid>, IWriteRepository<TourSchedule, Guid>
{
}
