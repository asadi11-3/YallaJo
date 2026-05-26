using ContentTours.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

public interface IGuideScheduleRepository : IReadRepository<GuideSchedule, Guid>, IWriteRepository<GuideSchedule, Guid>
{
    Task<IReadOnlyList<GuideSchedule>> GetByOfferingIdAsync(Guid offeringId, CancellationToken ct = default, bool asNoTracking = true);

    Task<IReadOnlyList<GuideSchedule>> GetByTourAndGuideAsync(Guid tourId, Guid tourGuideId, CancellationToken ct = default, bool asNoTracking = true);
}
