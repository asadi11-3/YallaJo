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

namespace ContentTours.Application.Commands.TourGuides.Assign;

public sealed class AssignTourGuideCommandHandler(
    ITourRepository tourRepo,
    ITourTourGuideRepository guideRepo,
    ITourGuideRepository guideProfileRepository,
    IContentToursUnitOfWork unitOfWork,
    IContentToursOutboxWriter outbox,
    IUserRoleChecker userRoleChecker,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<AssignTourGuideCommandHandler> logger)
    : ICommandHandler<AssignTourGuideCommand>
{
    public async Task<Result> Handle(
        AssignTourGuideCommand request, CancellationToken cancellationToken)
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
            if (tour.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                    new Error(
                        "Tour.NotOwner",
                        "You do not have permission to assign guides to this tour."),
                    Outcome.Forbidden);
            }

            var targetIsGuide = await userRoleChecker.HasRoleAsync(
                request.TourGuideUserId, AppRoles.TourGuide, cancellationToken);
            if (!targetIsGuide)
            {
                return Result.Failure(
                    new Error(
                        "TourTourGuide.NotAGuide",
                        $"User '{request.TourGuideUserId}' does not hold the TourGuide role."),
                    Outcome.Invalid);
            }

            // 4. Suspended/Archived: warn but allow (consistency with waypoint handlers — PDF B1.1)
            if (tour.Status is TourStatus.Suspended or TourStatus.Archived)
            {
                logger.LogWarning(
                    "Assigning guide to {Status} TourId={TourId} by UserId={UserId}",
                    tour.Status, tour.Id, currentUser.UserId);
            }

            var existingAssignments = await guideRepo.GetAllAsync(
                filter:       g => g.TourId == request.TourId,
                ct:           cancellationToken,
                asNoTracking: false);

            // 5a. Duplicate-assignment check
            if (existingAssignments.Any(g => g.TourGuideId == request.TourGuideUserId))
            {
                return Result.Failure(
                    new Error(
                        "TourTourGuide.AlreadyAssigned",
                        $"Guide '{request.TourGuideUserId}' is already assigned to this tour."),
                    Outcome.Conflict);
            }

            var existingCount = existingAssignments.Count;
            var effectivePrimary = request.IsPrimary || existingCount == 0;

            // 6a. Demote existing primary if we are taking over the primary slot.
            if (effectivePrimary && existingCount > 0)
            {
                var currentPrimary = existingAssignments.FirstOrDefault(g => g.IsPrimary);
                currentPrimary?.SetAsNonPrimary();
            }

            // 7. Create + add new assignment row.
            var newAssignment = TourTourGuide.Create(
                request.TourId, request.TourGuideUserId, effectivePrimary);
            await guideRepo.AddAsync(newAssignment, cancellationToken);

            // 8. Outbox enqueue BEFORE save so the event ships in the same transaction.
            outbox.Enqueue(new TourGuideAssignedIntegrationEvent(
                TourId:           tour.Id,
                TourGuideUserId:  request.TourGuideUserId,
                IsPrimary:        effectivePrimary,
                AssignedByUserId: currentUser.UserId!.Value));

            // 9. Save with concurrency-conflict catch
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict while assigning guide to TourId={TourId}", tour.Id);
                return Result.Failure(
                    new Error(
                        "TourTourGuide.ConcurrencyConflict",
                        "This tour's guide assignments were modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                TourGuideCacheKeys.TagForTour(tour.Id), cancellationToken);
            var guideProfile = await guideProfileRepository
                .GetByUserIdAsync(request.TourGuideUserId, cancellationToken)
                .ConfigureAwait(false);
            if (guideProfile is not null)
            {
                await cache.RemoveByTagAsync(
                    TourGuideCacheKeys.TagForProfile(guideProfile.Id), cancellationToken);
            }

            await cache.RemoveByTagAsync(
                ContentToursCacheKeys.TagForTour(tour.Id), cancellationToken);

            logger.LogInformation(
                "Assigned TourGuide {TourGuideUserId} to TourId={TourId} (IsPrimary={IsPrimary}, AutoPromoted={AutoPromoted}, AssignedBy={AssignedBy})",
                request.TourGuideUserId, tour.Id, effectivePrimary,
                effectivePrimary && !request.IsPrimary,
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
