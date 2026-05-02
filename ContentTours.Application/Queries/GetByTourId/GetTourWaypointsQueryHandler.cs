using ContentTours.Application.Queries.TourGuides.Common;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourWaypoints.GetByTourId;

public sealed class GetTourWaypointsQueryHandler(
    ITourWaypointRepository waypointRepo,
    ILogger<GetTourWaypointsQueryHandler> logger)
    : IQueryHandler<GetTourWaypointsQuery, IReadOnlyCollection<TourWaypointDto>>
{
    public async Task<Result<IReadOnlyCollection<TourWaypointDto>>> Handle(
        GetTourWaypointsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var waypoints = await waypointRepo
                .Query(filter: w => w.TourId == request.TourId)
                .OrderBy(w => w.SortOrder)
                .ToListAsync(cancellationToken);

            var dtos = waypoints
                .Select(w => new TourWaypointDto(
                    Id:              w.Id,
                    Name:            w.Name,
                    Description:     w.Description,
                    Latitude:        w.Location.Latitude,
                    Longitude:       w.Location.Longitude,
                    SortOrder:       w.SortOrder,
                    DurationMinutes: w.DurationMinutes,
                    WaypointType:    (byte)w.WaypointType))
                .ToList();

            logger.LogDebug(
                "Listed {Count} waypoints for TourId={TourId}",
                dtos.Count, request.TourId);

            return Result.Success<IReadOnlyCollection<TourWaypointDto>>(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyCollection<TourWaypointDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
