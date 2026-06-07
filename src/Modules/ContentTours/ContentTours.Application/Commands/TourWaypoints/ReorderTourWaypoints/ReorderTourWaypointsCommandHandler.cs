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

namespace ContentTours.Application.Commands.TourWaypoints.ReorderTourWaypoints;

public sealed class ReorderTourWaypointsCommandHandler(
    ITourRepository tourRepo,
    ITourWaypointRepository waypointRepo,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ReorderTourWaypointsCommandHandler> logger)
    : ICommandHandler<ReorderTourWaypointsCommand>
{
    public async Task<Result> Handle(
        ReorderTourWaypointsCommand request, CancellationToken cancellationToken)
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
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles) >= RolePrivilegeLevel.Admin;
            if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                    new Error("Tour.NotOwner",
                        "You do not have permission to reorder waypoints on this tour."),
                    Outcome.Forbidden);
            }

            // 3. Suspended/Archived: warn but allow (PDF B1.1 — consistency with Add/Remove)
            if (tour.Status is TourStatus.Suspended or TourStatus.Archived)
            {
                logger.LogWarning(
                    "Reordering waypoints on {Status} TourId={TourId} by UserId={UserId}",
                    tour.Status, tour.Id, currentUser.UserId);
            }

            // 4. Duplicate check (PDF B2: provided list must contain no duplicates)
            //    Done BEFORE set-equality so a provided [A, A] list against current {A, B}
            //    surfaces as the more specific Duplicates error rather than SetMismatch.
            if (request.WaypointIds.Distinct().Count() != request.WaypointIds.Count)
            {
                return Result.Failure(
                    new Error(
                        "TourWaypoint.ReorderDuplicates",
                        "The provided waypoint list contains duplicate ids."),
                    Outcome.Invalid);
            }

            // 5. Load all current waypoints tracked (asNoTracking: false — SetSortOrder mutations follow)
            var waypoints = await waypointRepo.GetAllAsync(
                filter: w => w.TourId == request.TourId,
                ct: cancellationToken,
                asNoTracking: false);

            // 6. Set-equality check (PDF B2: provided ids must exactly equal current ids).
            //    A reorder request that omits a waypoint or includes an unknown id is
            //    structurally invalid — clients must POST/DELETE first.
            var currentIds = waypoints.Select(w => w.Id).ToHashSet();
            var requestedIds = request.WaypointIds.ToHashSet();
            if (!currentIds.SetEquals(requestedIds))
            {
                return Result.Failure(
                    new Error(
                        "TourWaypoint.ReorderSetMismatch",
                        "The provided waypoint ids do not match the tour's current waypoints. " +
                        "Use POST to add or DELETE to remove before reordering."),
                    Outcome.Invalid);
            }

            // 7. Reassign SortOrder 0..N-1 in the requested order.
            // TODO(P2): wrap SetSortOrder loop + SaveChangesAsync in a serializable
            //           transaction (PDF B3) once shared-kernel tx plumbing is approved.
            var byId = waypoints.ToDictionary(w => w.Id);
            for (int i = 0; i < request.WaypointIds.Count; i++)
            {
                byId[request.WaypointIds[i]].SetSortOrder(i);
            }

            // 8. Save with concurrency-conflict catch
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict while reordering waypoints on TourId={TourId}", tour.Id);
                return Result.Failure(
                    new Error(
                        "TourWaypoint.ConcurrencyConflict",
                        "This tour's waypoints were modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            // 9. Cache invalidation (most-specific tag first per ERR-010)
            await cache.RemoveByTagAsync(
                TourWaypointCacheKeys.TagForTour(tour.Id), cancellationToken);
            await cache.RemoveByTagAsync(
                ContentToursCacheKeys.TagForTour(tour.Id), cancellationToken);

            logger.LogInformation(
                "Reordered {Count} waypoints on TourId={TourId} (dense 0..{Last})",
                request.WaypointIds.Count, tour.Id, request.WaypointIds.Count - 1);

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
