using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
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

namespace ContentTours.Application.Commands.TourWaypoints.UpdateTourWaypoint;

public sealed class UpdateTourWaypointCommandHandler(
    ITourRepository tourRepo,
    ITourWaypointRepository waypointRepo,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateTourWaypointCommandHandler> logger)
    : ICommandHandler<UpdateTourWaypointCommand>
{
    public async Task<Result> Handle(
        UpdateTourWaypointCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Load tour
            var tour = await tourRepo.GetByIdAsync(request.TourId, cancellationToken);
            if (tour is null || tour.IsDeleted)
            {
                return Result.Failure(
                    new Error("Tour.NotFound", $"Tour '{request.TourId}' was not found."),
                    Outcome.NotFound);
            }

            // 2. Ownership (admin tier bypass) — Plan rule #11
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles) >= RolePrivilegeLevel.Admin;
            if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                    new Error(
                        "Tour.NotOwner",
                        "You do not have permission to update waypoints on this tour."),
                    Outcome.Forbidden);
            }

            // 3. Allow but warn on Suspended/Archived (PDF B1.1 parity with Add)
            if (tour.Status is TourStatus.Suspended or TourStatus.Archived)
            {
                logger.LogWarning(
                    "Updating waypoint on {Status} TourId={TourId} by UserId={UserId}",
                    tour.Status, tour.Id, currentUser.UserId);
            }

            // 4. Coordinate sanity
            if (request.Latitude == 0d && request.Longitude == 0d)
            {
                return Result.Failure(
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

            // 6. Load waypoints for this tour (tracked so we can persist mutations)
            var existingWaypoints = await waypointRepo.GetAllAsync(
                filter: w => w.TourId == request.TourId,
                asNoTracking: false,
                ct: cancellationToken);

            var target = existingWaypoints.FirstOrDefault(w => w.Id == request.WaypointId);
            if (target is null)
            {
                return Result.Failure(
                    new Error(
                        "TourWaypoint.NotFound",
                        $"Waypoint '{request.WaypointId}' was not found on this tour."),
                    Outcome.NotFound);
            }

            // 7. Name uniqueness — exclude self (case-insensitive, in-memory)
            var nameConflict = existingWaypoints.Any(w =>
                w.Id != request.WaypointId &&
                w.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));
            if (nameConflict)
            {
                return Result.Failure(
                    new Error(
                        "TourWaypoint.NameConflict",
                        $"A waypoint named '{request.Name}' already exists on this tour."),
                    Outcome.Conflict);
            }

            // 8. WaypointType is not provider-editable via this BFF surface.
            //    BFF's `IsMeetingPoint` boolean has no direct domain enum equivalent
            //    (domain values: Start, Stop, Meal, Photo, LandMark, RestStop, End).
            //    Preserve the existing type; a separate admin/specialized surface would be
            //    needed to change waypoint type once it has been chosen.

            // 9. Apply domain mutation
            var location = new Location((decimal)request.Latitude, (decimal)request.Longitude);
            target.Update(
                name:            request.Name,
                description:     request.Description,
                location:        location,
                durationMinutes: request.StopDurationMinutes,
                waypointType:    target.WaypointType);

            // 10. Persist
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict while updating waypoint {WaypointId} on TourId={TourId}",
                    request.WaypointId, tour.Id);
                return Result.Failure(
                    new Error(
                        "TourWaypoint.ConcurrencyConflict",
                        "This tour's waypoints were modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            // 11. Cache invalidation (most-specific tag first per ERR-010)
            await cache.RemoveByTagAsync(
                TourWaypointCacheKeys.TagForTour(tour.Id), cancellationToken);
            await cache.RemoveByTagAsync(
                ContentToursCacheKeys.TagForTour(tour.Id), cancellationToken);

            logger.LogInformation(
                "Updated TourWaypoint {WaypointId} '{Name}' on TourId={TourId} Type={Type}",
                target.Id, target.Name, tour.Id, target.WaypointType);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
