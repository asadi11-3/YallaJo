using ContentPlaces.Application.Queries.Place.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Place.GetNearbyPlaces;

public sealed class GetNearbyPlacesQueryHandler(
    IPlaceRepository placeRepository,
    ILogger<GetNearbyPlacesQueryHandler> logger)
    : IQueryHandler<GetNearbyPlacesQuery, IReadOnlyList<NearbyPlaceSummaryDto>>
{
    public async Task<Result<IReadOnlyList<NearbyPlaceSummaryDto>>> Handle(
        GetNearbyPlacesQuery request,
        CancellationToken cancellationToken)
    {
        // Pre-filter with bounding box (LINQ), then refine with Haversine in-memory
        var degreeOffset = request.RadiusKm / 111.0;
        var minLat = (decimal)(request.Lat - degreeOffset);
        var maxLat = (decimal)(request.Lat + degreeOffset);
        var minLng = (decimal)(request.Lng - degreeOffset);
        var maxLng = (decimal)(request.Lng + degreeOffset);

        var candidates = await placeRepository.GetAllAsync(
            filter: p => !p.IsDeleted &&
                         p.Location.Latitude != 0.0m && p.Location.Longitude != 0.0m &&
                         p.Location.Latitude >= minLat && p.Location.Latitude <= maxLat &&
                         p.Location.Longitude >= minLng && p.Location.Longitude <= maxLng,
            ct: cancellationToken);

        var results = candidates
            .Select(p => new
            {
                Place = p,
                Distance = CalculateDistance(
                    request.Lat, request.Lng,
                    (double)p.Location.Latitude, (double)p.Location.Longitude)
            })
            .Where(x => x.Distance <= request.RadiusKm)
            .OrderBy(x => x.Distance)
            .Take(request.PageSize)
            .Select(x => new NearbyPlaceSummaryDto(
                x.Place.Id,
                x.Place.Name,
                null,
                (double)x.Place.AverageRating,
                x.Distance,
                x.Place.PlaceType.ToString()))
            .ToList();

        logger.LogInformation(
            "Found {Count} nearby places for ({Lat},{Lng}) within {Radius}km",
            results.Count, request.Lat, request.Lng, request.RadiusKm);

        return Result<IReadOnlyList<NearbyPlaceSummaryDto>>.Success(results);
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
