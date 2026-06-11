using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourGuides.RemoveSpecialization;

public sealed class RemoveTourGuideSpecializationCommandHandler(
    ITourGuideRepository guideRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RemoveTourGuideSpecializationCommandHandler> logger)
    : ICommandHandler<RemoveTourGuideSpecializationCommand>
{
    public async Task<Result> Handle(RemoveTourGuideSpecializationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is not Guid callerUserId)
            {
                return Result.Failure(
                    new Error("Auth.Unauthorized", "An authenticated user is required."),
                    Outcome.Unauthorized);
            }

            var guide = await guideRepository
                .GetWithDetailsAsync(request.TourGuideId, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);

            if (guide is null)
            {
                return Result.Failure(
                    new Error("TourGuide.NotFound", $"Tour guide '{request.TourGuideId}' was not found."),
                    Outcome.NotFound);
            }

            if (guide.UserId != callerUserId)
            {
                return Result.Failure(
                    new Error("TourGuide.NotOwner", "You do not have permission to update this guide profile."),
                    Outcome.Forbidden);
            }

            var removeResult = guide.RemoveSpecialization(request.SpecializationId);
            if (removeResult.IsFailure)
            {
                var error = removeResult.Errors.FirstOrDefault()
                    ?? new Error("TourGuideSpecialization.InvalidState", "Unable to remove the specialization.");

                var outcome = error.Code == "TourGuideSpecialization.NotFound"
                    ? Outcome.NotFound
                    : Outcome.Conflict;

                return Result.Failure(error, outcome);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict removing specialization from TourGuideId={TourGuideId}", guide.Id);
                return Result.Failure(
                    new Error("TourGuide.ConcurrencyConflict", "This guide profile was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForProfile(guide.Id), cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Removed SpecializationId={SpecializationId} from TourGuideId={TourGuideId} by UserId={UserId}",
                request.SpecializationId,
                guide.Id,
                callerUserId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
