using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Place.GetMapViewport;

public sealed class GetMapViewportQueryHandler(
    IPlaceRepository placeRepository,
    ILogger<GetMapViewportQueryHandler> logger)
    : IQueryHandler<GetMapViewportQuery, MapViewportResponse>
{
    public async Task<Result<MapViewportResponse>> Handle(
        GetMapViewportQuery request,
        CancellationToken cancellationToken)
    {
        var places = await placeRepository.GetAllAsync(
            filter: p => !p.IsDeleted &&
                         p.Location.Latitude != 0.0m && p.Location.Longitude != 0.0m &&
                         p.Location.Latitude <= (decimal)request.NorthLat &&
                         p.Location.Latitude >= (decimal)request.SouthLat &&
                         p.Location.Longitude <= (decimal)request.EastLng &&
                         p.Location.Longitude >= (decimal)request.WestLng,
            ct: cancellationToken);

        var pins = places.Select(p => new MapPinDto(
            p.Id,
            p.Name,
            (double)p.Location.Latitude,
            (double)p.Location.Longitude,
            null,
            (double)p.AverageRating,
            0)).ToList();

        var isClusteringRecommended = pins.Count > 50;

        logger.LogInformation(
            "Viewport returned {Count} pins, clustering={Clustering}",
            pins.Count, isClusteringRecommended);

        return Result<MapViewportResponse>.Success(
            new MapViewportResponse(pins, isClusteringRecommended));
    }
}
