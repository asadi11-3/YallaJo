using ContentTours.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

public interface IGuidePricingTierRepository : IReadRepository<GuidePricingTier, Guid>, IWriteRepository<GuidePricingTier, Guid>
{
    Task<IReadOnlyList<GuidePricingTier>> GetByOfferingIdAsync(Guid offeringId, CancellationToken ct = default, bool asNoTracking = true);

    Task<IReadOnlyList<GuidePricingTier>> GetByTourAndGuideAsync(Guid tourId, Guid tourGuideId, CancellationToken ct = default, bool asNoTracking = true);
}
