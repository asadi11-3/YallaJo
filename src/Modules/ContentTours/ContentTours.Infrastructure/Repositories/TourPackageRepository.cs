using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentTours.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITourPackageRepository"/>.
/// Inherits the standard read/write surface from <see cref="EfEntityRepository{TEntity,TKey}"/>
/// and adds Task 5–specific eager loads, duplicate checks, and summary projections.
/// </summary>
internal sealed class TourPackageRepository(ContentToursDbContext context)
    : EfEntityRepository<TourPackage, Guid>(context), ITourPackageRepository
{
    private readonly ContentToursDbContext _context = context;

    public Task<TourPackage?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct)
    {
        return _context.TourPackages
            .AsNoTracking()
            .Include(p => p.IncludedTours)
                .ThenInclude(link => link.Tour)
            .Include(p => p.Inclusions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task<bool> InclusionDescriptionExistsAsync(
        Guid tourPackageId,
        string description,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Task.FromResult(false);

        var trimmed = description.Trim();

        return _context.TourPackageInclusions
            .AsNoTracking()
            .AnyAsync(
                i => i.TourPackageId == tourPackageId
                  && i.Description == trimmed,
                ct);
    }

    public async Task<(IReadOnlyList<TourPackageSummaryRow> Items, int TotalCount)> GetPagedSummariesAsync(
        int page,
        int pageSize,
        Guid? providerId,
        decimal? minPrice,
        decimal? maxPrice,
        string? currency,
        Guid? includeTourId,
        DateTime effectiveDateUtc,
        TourPackageSortOption sort,
        CancellationToken ct)
    {
        // Base query: AsNoTracking + soft-delete filter (auto) + exclude expired packages.
        var query = _context.TourPackages
            .AsNoTracking()
            .Where(p => p.ValidTo == null || p.ValidTo >= effectiveDateUtc);

        if (providerId.HasValue && providerId.Value != Guid.Empty)
            query = query.Where(p => p.CreatedByUserId == providerId.Value);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price.Amount >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price.Amount <= maxPrice.Value);

        if (!string.IsNullOrWhiteSpace(currency))
        {
            var normalized = currency.Trim().ToUpperInvariant();
            query = query.Where(p => p.Currency == normalized);
        }

        if (includeTourId.HasValue && includeTourId.Value != Guid.Empty)
        {
            query = query.Where(p => p.IncludedTours.Any(link => link.TourId == includeTourId.Value));
        }

        var total = await query.CountAsync(ct).ConfigureAwait(false);

        IOrderedQueryable<TourPackage> ordered = sort switch
        {
            TourPackageSortOption.PriceAscending      => query.OrderBy(p => p.Price.Amount),
            TourPackageSortOption.PriceDescending     => query.OrderByDescending(p => p.Price.Amount),
            TourPackageSortOption.ValidityEndingSoon  => query.OrderBy(p => p.ValidTo ?? DateTime.MaxValue),
            _                                         => query.OrderByDescending(p => p.CreatedAt),
        };

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new TourPackageSummaryRow(
                p.Id,
                p.CreatedByUserId,
                p.Name,
                p.Description,
                p.Price.Amount,
                p.Currency,
                p.MaxParticipants,
                p.ValidFrom,
                p.ValidTo,
                p.IncludedTours.Count,
                p.CreatedAt,
                p.CoverImageUrl))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, total);
    }
}
