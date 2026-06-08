using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourGuides.UpdateProfile;

public sealed class UpdateTourGuideProfileCommandHandler(
    ITourGuideRepository guideRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateTourGuideProfileCommandHandler> logger)
    : ICommandHandler<UpdateTourGuideProfileCommand>
{
    public async Task<Result> Handle(UpdateTourGuideProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is not Guid callerUserId)
            {
                return Result.Failure(
                    new Error("Auth.Unauthorized", "Authentication is required."),
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

            try
            {
                guide.UpdateProfile(
                    request.Bio,
                    request.YearsOfExperience,
                    request.HasFirstAid,
                    request.MoTALicenseNumber);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(new Error("TourGuide.InvalidProfile", ex.Message), Outcome.Invalid);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(new Error("TourGuide.InvalidState", ex.Message), Outcome.Conflict);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating TourGuideId={TourGuideId}", guide.Id);
                return Result.Failure(
                    new Error("TourGuide.ConcurrencyConflict", "This guide profile was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForProfile(guide.Id), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Updated TourGuide profile {TourGuideId} by UserId={UserId}", guide.Id, callerUserId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
