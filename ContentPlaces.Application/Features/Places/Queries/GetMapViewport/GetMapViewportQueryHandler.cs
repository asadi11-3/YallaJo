using ContentPlaces.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.Places.Queries.GetMapViewport;

public sealed class GetMapViewportQueryHandler : IRequestHandler<GetMapViewportQuery, MapViewportResponse>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<GetMapViewportQueryHandler> _logger;

    public GetMapViewportQueryHandler(
        IDbContext dbContext,
        ILogger<GetMapViewportQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<MapViewportResponse> Handle(GetMapViewportQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Getting map viewport places for Box: North {North}, South {South}, East {East}, West {West}",
            request.NorthLat, request.SouthLat, request.EastLng, request.WestLng);

        var efContext = (DbContext)_dbContext;

        var placesInViewport = await efContext.Set<Place>()
            .Where(p => !p.IsDeleted &&
                        p.Location.Latitude != 0.0m && p.Location.Longitude != 0.0m && 
                        p.Location.Latitude <= (decimal)request.NorthLat &&
                        p.Location.Latitude >= (decimal)request.SouthLat &&
                        p.Location.Longitude <= (decimal)request.EastLng &&
                        p.Location.Longitude >= (decimal)request.WestLng)
            .AsNoTracking() 
            .ToListAsync(ct);

        var pins = placesInViewport.Select(p => new MapPinDto(
            p.Id,
            p.Name,
            (double)p.Location.Latitude,
            (double)p.Location.Longitude,
            null,
            (double)p.AverageRating,
            0
        )).ToList();

        bool isClusteringRecommended = pins.Count > 50;

        return new MapViewportResponse(pins, isClusteringRecommended);
    }
}
