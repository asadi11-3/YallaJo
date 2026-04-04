using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentPlaces.Infrastructure.Repositories;

internal sealed class PlaceRepository(ContentPlacesDbContext context)
    : EfRepository<Place, Guid>(context), IPlaceRepository
{

    public async Task<PaginatedResult<Place>> GetPagedAsync(
        int page,
        int pageSize,
        Guid? categoryId,
        decimal? ratingMin,
        decimal? ratingMax,
        string? city,
        string? country,
        CancellationToken ct = default)
    {
        var query = context.Places.AsNoTracking().AsQueryable();

        if (ratingMin.HasValue)
            query = query.Where(p => p.AverageRating >= ratingMin.Value);
        if (ratingMax.HasValue)
            query = query.Where(p => p.AverageRating <= ratingMax.Value);
        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(p => p.City == city);
        if (!string.IsNullOrWhiteSpace(country))
            query = query.Where(p => p.Country == country);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.AverageRating)
            .ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<Place>(items, totalCount, page, pageSize);
    }
}
