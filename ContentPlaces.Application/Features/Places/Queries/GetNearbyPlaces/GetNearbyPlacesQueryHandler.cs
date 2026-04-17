using ContentPlaces.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContentPlaces.Domain.Repositories;

namespace ContentPlaces.Application.Features.Places.Queries.GetNearbyPlaces;

public sealed class GetNearbyPlacesQueryHandler : IRequestHandler<GetNearbyPlacesQuery, List<NearbyPlaceSummaryDto>>
{
    private readonly IDbContext _dbContext;
    private readonly IPlaceRepository _paceRepository;
    private readonly ILogger<GetNearbyPlacesQueryHandler> _logger;

    public GetNearbyPlacesQueryHandler(
        IDbContext dbContext,
        IPlaceRepository placeRepository,
        ILogger<GetNearbyPlacesQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<NearbyPlaceSummaryDto>> Handle(GetNearbyPlacesQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Getting nearby places for Lat: {Lat}, Lng: {Lng}, Radius: {Radius}km", request.Lat, request.Lng, request.RadiusKm);

        var efContext = (DbContext)_dbContext;

      /*  var nearbyPlaces = await efContext.Set<Place>()
            .FromSqlInterpolated($@"
                SELECT * FROM content_places.Places
                WHERE IsDeleted = 0
                  AND (Location_Latitude != 0.0 AND Location_Longitude != 0.0)
                  AND (6371 * ACOS(
                        COS(RADIANS({request.Lat})) * COS(RADIANS(Location_Latitude)) * COS(RADIANS(Location_Longitude) - RADIANS({request.Lng})) + 
                        SIN(RADIANS({request.Lat})) * SIN(RADIANS(Location_Latitude))
                      )) <= {request.RadiusKm}
                ORDER BY (6371 * ACOS(
                            COS(RADIANS({request.Lat})) * COS(RADIANS(Location_Latitude)) * COS(RADIANS(Location_Longitude) - RADIANS({request.Lng})) + 
                            SIN(RADIANS({request.Lat})) * SIN(RADIANS(Location_Latitude))
                         )) ASC
            ")
            .Take(request.PageSize)
            .AsNoTracking()
            .ToListAsync(ct);
      */
        return nearbyPlaces.Select(p => new NearbyPlaceSummaryDto(
            p.Id,
            p.Name,
            null,
            (double)p.AverageRating,
            CalculateDistance(request.Lat, request.Lng, (double)p.Location.Latitude, (double)p.Location.Longitude),
            p.PlaceType.ToString()
        )).ToList();
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) +
                (Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2));

        var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));

        return Math.Round(6371 * c, 2);
    }

    private static double ToRadians(double angle) => Math.PI * angle / 180.0;
}
