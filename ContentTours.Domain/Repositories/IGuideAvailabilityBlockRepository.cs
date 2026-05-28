using ContentTours.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

public interface IGuideAvailabilityBlockRepository
    : IReadRepository<GuideAvailabilityBlock, Guid>, IWriteRepository<GuideAvailabilityBlock, Guid>
{
    Task<IReadOnlyList<GuideAvailabilityBlock>> GetByGuideIdAsync(Guid guideId, CancellationToken ct = default);
}
