using ContentTours.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

public interface ITourGuideRepository : IRepository<TourGuide, Guid>
{
    Task<TourGuide?> GetWithDetailsAsync(Guid id, CancellationToken ct = default, bool asNoTracking = true);

    Task<TourGuide?> GetByUserIdAsync(Guid userId, CancellationToken ct = default, bool asNoTracking = true);

    Task<int> CountAssignedToursAsync(Guid guideUserId, CancellationToken ct = default);
}
