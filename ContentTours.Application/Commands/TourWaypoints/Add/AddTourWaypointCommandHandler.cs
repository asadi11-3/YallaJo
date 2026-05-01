using System.Threading;
using System.Threading.Tasks;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Application.Features.TourWaypoints.Commands.Add;

internal sealed class AddTourWaypointCommandHandler : ICommandHandler<AddTourWaypointCommand>
{
    private readonly ITourWaypointRepository _waypointRepository;
    private readonly IContentToursUnitOfWork _unitOfWork;

    public AddTourWaypointCommandHandler(
        ITourWaypointRepository waypointRepository, 
        IContentToursUnitOfWork unitOfWork)
    {
        _waypointRepository = waypointRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddTourWaypointCommand request, CancellationToken cancellationToken)
    {
        var waypoints = await _waypointRepository.GetByTourIdAsync(request.TourId, cancellationToken);
        int newSortOrder = waypoints.Count;
        var location = new Location(request.Latitude, request.Longitude);

        var waypoint = TourWaypoint.Create(
            request.TourId,
            request.Name,
            request.Description,
            location,
            newSortOrder,
            request.DurationMinutes ?? 0,
            request.WaypointType);

        _waypointRepository.Add(waypoint);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
