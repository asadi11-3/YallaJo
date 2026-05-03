using ContentTours.Domain.Entities;

namespace ContentTours.Domain.Repositories;

public interface ITourPackageRepository
{
    Task<TourPackage?> GetByIdAsync(Guid id, CancellationToken ct);

    Task AddAsync(TourPackage package, CancellationToken ct);

    void Update(TourPackage package);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct);

    Task<List<TourPackage>> GetAllAsync(CancellationToken ct);

    Task<(List<TourPackage> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        bool? isActive,
        Guid? tourId,
        CancellationToken ct);
}
