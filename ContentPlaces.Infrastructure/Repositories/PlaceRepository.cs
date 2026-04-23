using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Queries;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentPlaces.Infrastructure.Repositories;

internal sealed class PlaceRepository(ContentPlacesDbContext context)
    : EfRepository<Place, Guid>(context), IPlaceRepository
{
    public Task<bool> HasActiveLinkedBusinessesAsync(Guid placeId, CancellationToken ct = default) =>
        context.PlaceBusinesses.AnyAsync(pb => pb.PlaceId == placeId, ct);

    public async Task<IReadOnlyList<NearbyPlaceResult>> GetNearbyAsync(
        double lat,
        double lng,
        double radiusKm,
        int pageSize,
        CancellationToken ct = default)
    {
        // ── Strategy ──────────────────────────────────────────────────────────
        // 1. Bounding-box pre-filter (cheap arithmetic, uses IX_Places_IsDeleted_Latitude_Longitude)
        //    eliminates the vast majority of rows before any trig is evaluated.
        // 2. Haversine formula runs in a derived table (computed ONCE per surviving row).
        //    The outer query filters and sorts on the pre-computed alias — avoids the
        //    classic double-computation anti-pattern where the full formula runs in both
        //    SELECT and WHERE.
        // 3. All {lat}, {lng}, {radiusKm}, {pageSize} are FormattableString holes —
        //    EF Core 8/9 SqlQuery<T>(FormattableString) converts every hole to a DbParameter,
        //    making this safe against SQL injection (identical to FromSqlInterpolated).
        //
        // Future: replace with NetTopologySuite geography + SPATIAL INDEX when dataset > ~50k rows.
        // ─────────────────────────────────────────────────────────────────────

        // Bounding-box offsets (1° latitude ≈ 111 km; longitude shrinks by cos(lat)).
        var latOffset  = radiusKm / 111.0;
        var lngOffset  = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));
        var minLat     = lat - latOffset;
        var maxLat     = lat + latOffset;
        var minLng     = lng - lngOffset;
        var maxLng     = lng + lngOffset;

        var results = await context.Database
            .SqlQuery<NearbyPlaceResult>($"""
                SELECT Id, Name, Slug, Latitude, Longitude, AverageRating, DistanceKm
                FROM (
                    SELECT
                        Id,
                        Name,
                        Slug,
                        Latitude,
                        Longitude,
                        AverageRating,
                        6371 * ACOS(
                            COS(RADIANS({lat})) * COS(RADIANS(Latitude))
                            * COS(RADIANS(Longitude) - RADIANS({lng}))
                            + SIN(RADIANS({lat})) * SIN(RADIANS(Latitude))
                        ) AS DistanceKm
                    FROM content_places.Places
                    WHERE IsDeleted   = 0
                      AND Latitude   <> 0.0
                      AND Longitude  <> 0.0
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
}
