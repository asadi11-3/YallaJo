using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Features.TourWaypoints.Commands.Reorder;

internal sealed class ReorderTourWaypointCommandHandler : ICommandHandler<ReorderTourWaypointCommand>
{
    private readonly ITourWaypointRepository _waypointRepository;
    private readonly IContentToursUnitOfWork _unitOfWork;

    public ReorderTourWaypointCommandHandler(ITourWaypointRepository waypointRepository, IContentToursUnitOfWork unitOfWork)
    {
        _waypointRepository = waypointRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReorderTourWaypointCommand request, CancellationToken cancellationToken)
    {
        var waypoints = await _waypointRepository.GetByTourIdAsync(request.TourId, cancellationToken);

        if (!waypoints.Any()) return Result.Failure(new Error("TourWaypoint.NotFound", "No waypoints found."));

        var waypointToMove = waypoints.FirstOrDefault(w => w.Id == request.WaypointId);
        if (waypointToMove is null) return Result.Failure(new Error("TourWaypoint.NotFound", "Waypoint not found."));

        int oldSortOrder = waypointToMove.SortOrder;
        int newSortOrder = request.NewSortOrder;

        if (oldSortOrder == newSortOrder) return Result.Success();
        if (newSortOrder < 0 || newSortOrder >= waypoints.Count) return Result.Failure(new Error("TourWaypoint.Invalid", "Out of bounds."));

        if (newSortOrder > oldSortOrder)
        {
            var waypointsToShift = waypoints.Where(w => w.SortOrder > oldSortOrder && w.SortOrder <= newSortOrder);
            foreach (var wp in waypointsToShift) wp.SetSortOrder(wp.SortOrder - 1);
        }
        else
        {
            var waypointsToShift = waypoints.Where(w => w.SortOrder >= newSortOrder && w.SortOrder < oldSortOrder);
            foreach (var wp in waypointsToShift) wp.SetSortOrder(wp.SortOrder + 1);
        }

        waypointToMove.SetSortOrder(newSortOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
