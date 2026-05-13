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

namespace ContentTours.Application.Commands.TourWaypoints.RemoveTourWaypoint;

public sealed class RemoveTourWaypointCommandHandler(
    ITourRepository tourRepo,
    ITourWaypointRepository waypointRepo,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RemoveTourWaypointCommandHandler> logger)
    : ICommandHandler<RemoveTourWaypointCommand>
{
    public async Task<Result> Handle(
        RemoveTourWaypointCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Load parent Tour
            var tour = await tourRepo.GetByIdAsync(request.TourId, cancellationToken);
            if (tour is null || tour.IsDeleted)
            {
                return Result.Failure(
                    new Error("Tour.NotFound", $"Tour '{request.TourId}' was not found."),
                    Outcome.NotFound);
            }

            // 2. Owner-or-admin gate (canonical pattern — Mahmoud DeleteTourCommandHandler:45-52)
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;
            if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                    new Error(
                        "Tour.NotOwner",
                        "You do not have permission to remove waypoints from this tour."),
                    Outcome.Forbidden);
            }

            // 3. Suspended/Archived: warn but allow (PDF B1.1 — consistent with Add)
            if (tour.Status is TourStatus.Suspended or TourStatus.Archived)
            {
                logger.LogWarning(
                    "Removing waypoint from {Status} TourId={TourId} by UserId={UserId}",
                    tour.Status, tour.Id, currentUser.UserId);
            }

            // 4. Load all waypoints tracked (asNoTracking: false — SetSortOrder mutations follow)
            var waypoints = await waypointRepo.GetAllAsync(
                filter: w => w.TourId == request.TourId,
                ct: cancellationToken,
                asNoTracking: false);

            // 5. Find target; 404 if missing or belongs to a different tour
            var target = waypoints.FirstOrDefault(w => w.Id == request.WaypointId);
            if (target is null)
            {
                return Result.Failure(
                    new Error(
                        "TourWaypoint.NotFound",
                        $"Waypoint '{request.WaypointId}' was not found on this tour."),
                    Outcome.NotFound);
            }

            // 6. Remove and re-compact SortOrder for remaining rows.
            // TODO(P2): wrap Remove + re-compact + SaveChangesAsync in a serializable
            //           transaction (PDF B3) once shared-kernel tx plumbing is approved.
            var deletedSortOrder = target.SortOrder;
            waypointRepo.Remove(target);

            foreach (var wp in waypoints.Where(w => w.SortOrder > deletedSortOrder))
            {
                wp.SetSortOrder(wp.SortOrder - 1);
            }

            // 7. Save with concurrency-conflict catch
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict while removing waypoint from TourId={TourId}", tour.Id);
                return Result.Failure(
                    new Error("TourWaypoint.ConcurrencyConflict",
                        "This tour's waypoints were modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            // 8. Cache invalidation (most-specific tag first per ERR-010)
            await cache.RemoveByTagAsync(
                TourWaypointCacheKeys.TagForTour(tour.Id), cancellationToken);
            await cache.RemoveByTagAsync(
                ContentToursCacheKeys.TagForTour(tour.Id), cancellationToken);

            logger.LogInformation(
                "Removed TourWaypoint {WaypointId} from TourId={TourId}; " +
                "re-compacted SortOrder for {ShiftCount} remaining waypoint(s)",
                request.WaypointId, tour.Id,
                waypoints.Count(w => w.SortOrder >= deletedSortOrder) - 1);

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
