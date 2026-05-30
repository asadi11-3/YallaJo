using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

public interface IGuideApplicationRepository : IRepository<GuideApplication, Guid>
{
    Task<GuideApplication?> GetWithDetailsAsync(Guid id, CancellationToken ct = default, bool asNoTracking = true);

    Task<bool> HasPendingApplicationAsync(Guid tourId, Guid tourGuideId, CancellationToken ct = default);

    Task<int> CountApplicationsAsync(Guid tourGuideId, GuideApplicationStatus? statusFilter = null, CancellationToken ct = default);

    Task<IReadOnlyList<GuideApplication>> GetByTourIdAsync(Guid tourId, GuideApplicationStatus? statusFilter = null, CancellationToken ct = default);

    Task<IReadOnlyList<GuideApplication>> GetByGuideIdAsync(Guid tourGuideId, GuideApplicationStatus? statusFilter = null, CancellationToken ct = default);
}
