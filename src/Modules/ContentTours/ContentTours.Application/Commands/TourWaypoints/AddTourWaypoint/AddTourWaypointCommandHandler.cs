using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Application.Commands.TourWaypoints.AddTourWaypoint;

public sealed class AddTourWaypointCommandHandler(
    ITourRepository tourRepo,
    ITourWaypointRepository waypointRepo,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<AddTourWaypointCommandHandler> logger)
    : ICommandHandler<AddTourWaypointCommand, AddTourWaypointResult>
{
    public async Task<Result<AddTourWaypointResult>> Handle(
        AddTourWaypointCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourRepo.GetByIdAsync(request.TourId, cancellationToken);
            if (tour is null || tour.IsDeleted)
            {
                return Result<AddTourWaypointResult>.Failure(
                    new Error("Tour.NotFound", $"Tour '{request.TourId}' was not found."),
                    Outcome.NotFound);
            }

            if (tour.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result<AddTourWaypointResult>.Failure(
                    new Error(
                        "Tour.NotOwner",
                        "You do not have permission to add waypoints to this tour."),
                    Outcome.Forbidden);
            }

            if (tour.Status is TourStatus.Suspended or TourStatus.Archived)
            {
                logger.LogWarning(
                    "Adding waypoint to {Status} TourId={TourId} by UserId={UserId}",
                    tour.Status, tour.Id, currentUser.UserId);
            }

            if (request.Latitude == 0d && request.Longitude == 0d)
            {
                return Result<AddTourWaypointResult>.Failure(
                    new Error(
                        "TourWaypoint.InvalidLocation",
                        "Coordinates (0, 0) are not a valid waypoint location."),
                    Outcome.Invalid);
            }

            // 5. Jordan bbox advisory (PDF B1.1: warn, don't block)
            const double JordanLatMin = 29.0;
            const double JordanLatMax = 33.5;
            const double JordanLngMin = 34.8;
            const double JordanLngMax = 39.4;
            if (request.Latitude < JordanLatMin || request.Latitude > JordanLatMax ||
                request.Longitude < JordanLngMin || request.Longitude > JordanLngMax)
            {
                logger.LogWarning(
                    "Waypoint coordinates ({Lat},{Lng}) outside Jordan bbox; allowing per PDF B1.1.",
                    request.Latitude, request.Longitude);
            }

          
            var existingWaypoints = await waypointRepo.GetAllAsync(
                filter: w => w.TourId == request.TourId,
                ct: cancellationToken);

            // 6a. Name uniqueness (case-insensitive, in-memory per Mohammad's precedent)
            var nameConflict = existingWaypoints.Any(w =>
                w.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));
            if (nameConflict)
            {
                return Result<AddTourWaypointResult>.Failure(
                    new Error(
                        "TourWaypoint.NameConflict",
                        $"A waypoint named '{request.Name}' already exists on this tour."),
                    Outcome.Conflict);
            }

            // 6b. Single-Start invariant (PDF B1: exactly one Start per tour)
            if (request.WaypointType == WaypointType.Start &&
                existingWaypoints.Any(w => w.WaypointType == WaypointType.Start))
            {
                return Result<AddTourWaypointResult>.Failure(
                    new Error(
                        "TourWaypoint.InvalidType",
                        "A Start waypoint already exists; only one Start is allowed per tour."),
                    Outcome.Conflict);
            }

            // 6c. Single-End invariant (PDF B1: exactly one End per tour)
            if (request.WaypointType == WaypointType.End &&
                existingWaypoints.Any(w => w.WaypointType == WaypointType.End))
            {
                return Result<AddTourWaypointResult>.Failure(
                    new Error(
                        "TourWaypoint.InvalidType",
                        "An End waypoint already exists; only one End is allowed per tour."),
                    Outcome.Conflict);
            }

            var maxSortOrder = await waypointRepo
                .Query(filter: w => w.TourId == request.TourId)
                .MaxAsync(w => (int?)w.SortOrder, cancellationToken) ?? -1;
            var newSortOrder = maxSortOrder + 1;

            var location = new Location((decimal)request.Latitude, (decimal)request.Longitude);
            var waypoint = TourWaypoint.Create(
                tourId:          request.TourId,
                name:            request.Name,
                description:     request.Description,
                location:        location,
                waypointType:    request.WaypointType,
                sortOrder:       newSortOrder,
                durationMinutes: request.DurationMinutes);

            await waypointRepo.AddAsync(waypoint, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict while adding waypoint to TourId={TourId}", tour.Id);
                return Result<AddTourWaypointResult>.Failure(
                    new Error(
                        "TourWaypoint.ConcurrencyConflict",
                        "This tour's waypoints were modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            // 10. Cache invalidation (most-specific tag first per ERR-010)
            await cache.RemoveByTagAsync(
                TourWaypointCacheKeys.TagForTour(tour.Id), cancellationToken);
            await cache.RemoveByTagAsync(
                ContentToursCacheKeys.TagForTour(tour.Id), cancellationToken);


            logger.LogInformation(
                "Added TourWaypoint {WaypointId} '{Name}' to TourId={TourId} at SortOrder={SortOrder} Type={Type}",
                waypoint.Id, waypoint.Name, tour.Id, waypoint.SortOrder, waypoint.WaypointType);

            return Result.Created(new AddTourWaypointResult(
                WaypointId: waypoint.Id,
                SortOrder:  waypoint.SortOrder));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<AddTourWaypointResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
