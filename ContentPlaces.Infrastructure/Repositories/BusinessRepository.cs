using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Queries;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentPlaces.Infrastructure.Repositories;

internal sealed class BusinessRepository(ContentPlacesDbContext context)
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
            .AsSplitQuery()
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

    public async Task<IReadOnlyList<NearbyBusinessResult>> GetNearbyAsync(
        double lat,
        double lng,
        double radiusKm,
        int pageSize,
        CancellationToken ct = default)
    {
        // Bounding-box offsets (1° latitude ≈ 111 km; longitude shrinks by cos(lat)).
        var latOffset = radiusKm / 111.0;
        var lngOffset = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));
        var minLat = lat - latOffset;
        var maxLat = lat + latOffset;
        var minLng = lng - lngOffset;
        var maxLng = lng + lngOffset;

        var results = await context.Database
            .SqlQuery<NearbyBusinessResult>($"""
                SELECT Id, Name, Slug, BusinessType, Status,
                       Latitude, Longitude,
                       City, Country, AverageRating, ReviewCount, IsVerified, IsFeatured,
                       DistanceKm
                FROM (
                    SELECT
                        Id, Name, Slug,
                        BusinessType,
                        Status,
                        Latitude, Longitude,
                        City, Country, AverageRating, ReviewCount, IsVerified, IsFeatured,
                        6371 * ACOS(
                            COS(RADIANS({lat})) * COS(RADIANS(Latitude))
                            * COS(RADIANS(Longitude) - RADIANS({lng}))
                            + SIN(RADIANS({lat})) * SIN(RADIANS(Latitude))
                        ) AS DistanceKm
                    FROM content_places.Businesses
                    WHERE IsDeleted           = 0
                      AND Status              = 1
                      AND Latitude  <> 0.0
                      AND Longitude <> 0.0
                      AND Latitude   BETWEEN {minLat} AND {maxLat}
                      AND Longitude  BETWEEN {minLng} AND {maxLng}
                ) AS candidate
                WHERE DistanceKm <= {radiusKm}
                ORDER BY DistanceKm
                OFFSET 0 ROWS FETCH NEXT {pageSize} ROWS ONLY
                """)
            .AsNoTracking()
            .ToListAsync(ct);

        return results;
    }

    public async Task<IReadOnlyList<Business>> GetByOwnerIdAsync(
        Guid ownerUserId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        return await context.Set<Business>()
            .Where(b => b.OwnerId == ownerUserId)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
