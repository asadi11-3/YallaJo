using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
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

namespace ContentTours.Application.Commands.TourGuides.Unassign;

public sealed class UnassignTourGuideCommandHandler(
    ITourRepository tourRepo,
    ITourTourGuideRepository guideRepo,
    IContentToursUnitOfWork unitOfWork,
    IContentToursOutboxWriter outbox,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UnassignTourGuideCommandHandler> logger)
    : ICommandHandler<UnassignTourGuideCommand>
{
    public async Task<Result> Handle(
        UnassignTourGuideCommand request, CancellationToken cancellationToken)
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
                        "You do not have permission to unassign guides from this tour."),
                    Outcome.Forbidden);
            }

            // 3. Suspended/Archived: warn but allow (PDF B1.1 — consistency with Assign)
            if (tour.Status is TourStatus.Suspended or TourStatus.Archived)
            {
                logger.LogWarning(
                    "Unassigning guide from {Status} TourId={TourId} by UserId={UserId}",
                    tour.Status, tour.Id, currentUser.UserId);
            }

            // 4. Load all guides for the tour (tracked — promotion mutations follow).
            var allGuides = await guideRepo.GetAllAsync(
                filter:       g => g.TourId == request.TourId,
                ct:           cancellationToken,
                asNoTracking: false);

            // 5. Find target by composite key (TourId + TourGuideId).
            var target = allGuides.FirstOrDefault(g =>
                g.TourGuideId == request.TourGuideUserId);
            if (target is null)
            {
                return Result.Failure(
                    new Error("TourTourGuide.NotFound",
                        $"Guide '{request.TourGuideUserId}' is not assigned to this tour."),
                    Outcome.NotFound);
            }

            // 6. Capture state BEFORE mutation (for the integration event + log).
            var wasPrimary = target.IsPrimary;

            guideRepo.Remove(target);

            TourTourGuide? newPrimary = null;
            if (wasPrimary)
            {
                var remaining = allGuides
                    .Where(g => g.TourGuideId != request.TourGuideUserId)
                    .OrderBy(g => g.TourGuideId)
                    .ToList();

                if (remaining.Count > 0)
                {
                    newPrimary = remaining[0];
                    newPrimary.SetAsPrimary();
                }
            }

            outbox.Enqueue(new TourGuideUnassignedIntegrationEvent(
                TourId:              tour.Id,
                TourGuideUserId:     request.TourGuideUserId,
                UnassignedByUserId:  currentUser.UserId!.Value));

            // 10. Save with concurrency-conflict catch
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict while unassigning guide from TourId={TourId}", tour.Id);
                return Result.Failure(
                    new Error(
                        "TourTourGuide.ConcurrencyConflict",
                        "This tour's guides were modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                TourGuideCacheKeys.TagForTour(tour.Id), cancellationToken);
            await cache.RemoveByTagAsync(
                ContentToursCacheKeys.TagForTour(tour.Id), cancellationToken);

            logger.LogInformation(
                "Unassigned TourGuide {TourGuideId} from TourId={TourId}; " +
                "wasPrimary={WasPrimary}; newPrimaryId={NewPrimaryId}; UnassignedBy={UnassignedBy}",
                request.TourGuideUserId, tour.Id, wasPrimary,
                newPrimary?.TourGuideId.ToString() ?? "<none>",
                currentUser.UserId);

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
