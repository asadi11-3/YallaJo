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
        // Haversine is computed in SQL Server via IPlaceRepository.GetNearbyAsync.
        // No in-memory distance calculation — all filtering and ordering happens in the DB.
        var rawResults = await placeRepository.GetNearbyAsync(
            request.Lat, request.Lng, request.RadiusKm, request.PageSize, cancellationToken);

        var dtos = rawResults
            .Select(r => new NearbyPlaceSummaryDto(
                r.Id,
                r.Name,
                r.Slug,
                r.Latitude,
                r.Longitude,
                r.AverageRating,
                r.DistanceKm))
            .ToList();

        logger.LogInformation(
            "Found {Count} nearby places for ({Lat},{Lng}) within {Radius}km",
            dtos.Count, request.Lat, request.Lng, request.RadiusKm);

        return Result<IReadOnlyList<NearbyPlaceSummaryDto>>.Success(dtos);
    }
}
