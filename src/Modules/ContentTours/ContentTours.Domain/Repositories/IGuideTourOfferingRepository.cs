using ContentTours.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

// GuideTourOffering is AuditableEntity (not IAggregateRoot), so we use IReadRepository + IWriteRepository directly
public interface IGuideTourOfferingRepository : IReadRepository<GuideTourOffering, Guid>, IWriteRepository<GuideTourOffering, Guid>
{
    Task<GuideTourOffering?> GetByTourAndGuideAsync(Guid tourId, Guid tourGuideId, CancellationToken ct = default, bool asNoTracking = true);

    Task<int> CountActiveOfferingsAsync(Guid tourId, CancellationToken ct = default);

    Task<IReadOnlyList<GuideTourOffering>> GetByTourIdAsync(Guid tourId, CancellationToken ct = default, bool asNoTracking = true);

    Task<IReadOnlyList<GuideTourOffering>> GetByGuideIdAsync(Guid tourGuideId, CancellationToken ct = default, bool asNoTracking = true);
}
