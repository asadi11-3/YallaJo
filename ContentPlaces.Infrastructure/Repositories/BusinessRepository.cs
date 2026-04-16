using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentPlaces.Infrastructure.Repositories;

public sealed class BusinessRepository(ContentPlacesDbContext context)
    : EfRepository<Business, Guid>(context), IBusinessRepository
{
    public async Task AddPlaceBusinessJunctionAsync(Guid placeId, Guid businessId, CancellationToken ct = default)
    {
        var junction = PlaceBusiness.Create(placeId, businessId);
        await context.Set<PlaceBusiness>().AddAsync(junction, ct);
    }

    public async Task RemovePlaceBusinessJunctionAsync(Guid placeId, Guid businessId, CancellationToken ct = default)
    {
        var junction = await context.Set<PlaceBusiness>()
            .FirstOrDefaultAsync(pb => pb.PlaceId == placeId && pb.BusinessId == businessId, ct);

        if (junction is not null)
            context.Set<PlaceBusiness>().Remove(junction);
    }

    public Task<bool> PlaceExistsAsync(Guid placeId, CancellationToken ct = default)
        => context.Places.AnyAsync(p => p.Id == placeId, ct);

    public async Task<Business?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await context.Businesses
            .AsNoTracking()
            .Include(b => b.BusinessTranslations)
            .Include(b => b.BusinessHours)
            .Include(b => b.ServiceItems)
            .Include(b => b.Staff)
            .Include(b => b.Amenities)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<BusinessHours>> GetBusinessHoursAsync(Guid businessId, CancellationToken ct = default)
        => await context.BusinessHours
            .Where(h => h.BusinessId == businessId)
            .OrderBy(h => h.DayOfWeek)
            .ThenBy(h => h.OpenTime)
            .ToListAsync(ct);

    public async Task ReplaceBusinessHoursAsync(Guid businessId, IEnumerable<BusinessHours> newHours, CancellationToken ct = default)
    {
        await context.BusinessHours
            .Where(h => h.BusinessId == businessId)
            .ExecuteDeleteAsync(ct);

        await context.Set<BusinessHours>().AddRangeAsync(newHours, ct);
    }
}
