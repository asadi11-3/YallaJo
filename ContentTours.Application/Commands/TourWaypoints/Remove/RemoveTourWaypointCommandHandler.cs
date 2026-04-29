using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Features.TourWaypoints.Commands.Remove;

internal sealed class RemoveTourWaypointCommandHandler : ICommandHandler<RemoveTourWaypointCommand>
{
    private readonly ITourWaypointRepository _waypointRepository;
    private readonly IContentToursUnitOfWork _unitOfWork;

    public RemoveTourWaypointCommandHandler(ITourWaypointRepository waypointRepository, IContentToursUnitOfWork unitOfWork)
    {
        _waypointRepository = waypointRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveTourWaypointCommand request, CancellationToken cancellationToken)
    {
        var waypoints = await _waypointRepository.GetByTourIdAsync(request.TourId, cancellationToken);
        var waypointToRemove = waypoints.FirstOrDefault(w => w.Id == request.WaypointId);

        if (waypointToRemove is null)
            return Result.Failure(new Error("TourWaypoint.NotFound", "Waypoint not found."));

        int deletedSortOrder = waypointToRemove.SortOrder;
        _waypointRepository.Remove(waypointToRemove);

        var waypointsToShift = waypoints.Where(w => w.SortOrder > deletedSortOrder);
        foreach (var wp in waypointsToShift) wp.SetSortOrder(wp.SortOrder - 1);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
