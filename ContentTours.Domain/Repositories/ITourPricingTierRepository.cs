using ContentTours.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

/// <summary>
/// TourPricingTier is a non-aggregate child entity (BaseEntity, not IAggregateRoot).
/// Uses IReadRepository + IWriteRepository rather than IRepository which requires IAggregateRoot.
/// </summary>
public interface ITourPricingTierRepository : IReadRepository<TourPricingTier, Guid>, IWriteRepository<TourPricingTier, Guid>
{
}
