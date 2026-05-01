using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContentTours.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Features.TourWaypoints.Queries.GetByTourId;

internal sealed class GetTourWaypointsQueryHandler : IQueryHandler<GetTourWaypointsQuery, IReadOnlyCollection<TourWaypointResponse>>
{
    private readonly ITourWaypointRepository _waypointRepository;

    public GetTourWaypointsQueryHandler(ITourWaypointRepository waypointRepository)
    {
        _waypointRepository = waypointRepository;
    }

    public async Task<Result<IReadOnlyCollection<TourWaypointResponse>>> Handle(GetTourWaypointsQuery request, CancellationToken cancellationToken)
    {
        var waypoints = await _waypointRepository.GetByTourIdAsync(request.TourId, cancellationToken);

        var response = waypoints.Select(w => new TourWaypointResponse(
            w.Id, w.Name, w.Description, w.Location.Latitude, w.Location.Longitude, w.SortOrder, w.DurationMinutes, w.WaypointType.ToString()
        )).ToList();

        return Result.Success<IReadOnlyCollection<TourWaypointResponse>>(response);
    }
}
