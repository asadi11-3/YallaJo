using ContentPlaces.Application.Queries.Place.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Place.GetMapViewport;

public sealed class GetMapViewportQueryHandler(
    IPlaceRepository placeRepository,
    ILogger<GetMapViewportQueryHandler> logger)
    : IQueryHandler<GetMapViewportQuery, MapViewportResponse>
{
    /// <summary>
    /// Internal hard cap on viewport pins. Response shape is unchanged
    /// (no client-visible pageSize field), so we enforce a server-side maximum to
    /// protect against unbounded queries when zoomed out over dense regions.
    /// IsClusteringRecommended is set when results approach the cap so clients
    /// can switch to clustered rendering.
    /// </summary>
    private const int MaxPins = 500;

    public async Task<Result<MapViewportResponse>> Handle(
        GetMapViewportQuery request,
        CancellationToken cancellationToken)
    {
        // Project server-side to MapPinDto and cap the result set.
        // Place is soft-delete-filtered automatically via the EF query filter.
        var pins = await placeRepository.Query()
            .Where(p => p.Location.Latitude != 0.0m && p.Location.Longitude != 0.0m
                        && p.Location.Latitude <= (decimal)request.NorthLat
                        && p.Location.Latitude >= (decimal)request.SouthLat
                        && p.Location.Longitude <= (decimal)request.EastLng
                        && p.Location.Longitude >= (decimal)request.WestLng)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.AverageRating)
            .Take(MaxPins)
            .Select(p => new MapPinDto(
                p.Id,
                p.Name,
                (double)p.Location.Latitude,
                (double)p.Location.Longitude,
                null,
                (double)p.AverageRating,
                0))
            .ToListAsync(cancellationToken);

        // Clustering hint: trigger above 50 pins or when the cap was reached.
        var isClusteringRecommended = pins.Count > 50 || pins.Count >= MaxPins;

        logger.LogInformation(
            "Viewport returned {Count} pins (cap={Cap}), clustering={Clustering}",
            pins.Count, MaxPins, isClusteringRecommended);

        return Result<MapViewportResponse>.Success(
            new MapViewportResponse(pins, isClusteringRecommended));
    }
}
