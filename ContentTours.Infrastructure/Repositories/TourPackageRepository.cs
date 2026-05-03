using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentTours.Infrastructure.Repositories;

public sealed class TourPackageRepository : ITourPackageRepository
{
    private readonly ContentToursDbContext _context;

    public TourPackageRepository(ContentToursDbContext context)
    {
        _context = context;
    }

    public async Task<TourPackage?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.TourPackages
            .Include(p => p.Tour)
            .Include(p => p.TourPackageInclusions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task AddAsync(TourPackage package, CancellationToken ct)
    {
        await _context.TourPackages.AddAsync(package, ct);
    }

    public void Update(TourPackage package)
    {
        _context.TourPackages.Update(package);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct)
    {
        return await _context.TourPackages
            .AnyAsync(p => p.Id == id, ct);
    }

    public async Task<List<TourPackage>> GetAllAsync(CancellationToken ct)
    {
        return await _context.TourPackages
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<(List<TourPackage> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        bool? isActive,
        Guid? tourId,
        CancellationToken ct)
    {
        var query = _context.TourPackages.AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (tourId.HasValue)
            query = query.Where(p => p.TourId == tourId.Value);

        var orderedQuery = query.OrderByDescending(p => p.CreatedAt);

        var totalCount = await orderedQuery.CountAsync(ct);

        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
