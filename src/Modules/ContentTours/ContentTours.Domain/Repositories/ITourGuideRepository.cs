using ContentTours.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

public interface ITourGuideRepository : IRepository<TourGuide, Guid>
{
    Task<TourGuide?> GetWithDetailsAsync(Guid id, CancellationToken ct = default, bool asNoTracking = true);

    Task<TourGuide?> GetByUserIdAsync(Guid userId, CancellationToken ct = default, bool asNoTracking = true);

    /// <summary>
    /// Loads the TourGuide aggregate (including <see cref="TourGuide.Languages"/> and
    /// <see cref="TourGuide.Specializations"/>) by the owning user's identity.
    /// Used by GET /api/v1/guides/me (F15 fix).
    /// </summary>
    Task<TourGuide?> GetWithDetailsByUserIdAsync(Guid userId, CancellationToken ct = default, bool asNoTracking = true);

    Task<int> CountAssignedToursAsync(Guid guideUserId, CancellationToken ct = default);

    Task<TourGuide?> GetBySlugAsync(string slug, CancellationToken ct = default, bool asNoTracking = true);

    Task<bool> IsSlugTakenAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);

    Task<(IReadOnlyList<TourGuide> Items, int TotalCount)> ListActiveAsync(
        int page, int pageSize, CancellationToken ct = default);
}
