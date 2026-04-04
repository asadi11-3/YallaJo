using ContentPlaces.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentPlaces.Domain.Repositories;

public interface IPlaceRepository : IRepository<Place, Guid>
{
    Task<PaginatedResult<Place>> GetPagedAsync(
        int page,
        int pageSize,
        Guid? categoryId,
        decimal? ratingMin,
        decimal? ratingMax,
        string? city,
        string? country,
        CancellationToken ct = default);
}
