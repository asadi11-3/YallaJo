using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.GuideApplication.Apply;

public sealed class ApplyForTourCommandHandler(
    ITourRepository tourRepository,
    ITourGuideRepository guideRepository,
    IGuideApplicationRepository applicationRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ApplyForTourCommandHandler> logger)
    : ICommandHandler<ApplyForTourCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ApplyForTourCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUser.UserId!.Value;

            var tour = await tourRepository.GetByIdAsync(request.TourId, cancellationToken).ConfigureAwait(false);
            if (tour is null || tour.IsDeleted)
            {
                return Result<Guid>.Failure(new Error("Tour.NotFound", $"Tour '{request.TourId}' not found."), Outcome.NotFound);
            }

            if (!tour.IsOpenForApplications)
            {
                return Result<Guid>.Failure(new Error("Tour.NotOpenForApplications", "This tour is not accepting guide applications."), Outcome.Conflict);
            }

            var guide = await guideRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
            if (guide is null)
            {
                return Result<Guid>.Failure(new Error("TourGuide.NotFound", "You do not have a tour guide profile."), Outcome.NotFound);
            }

            // Check for existing pending application
            var hasPending = await applicationRepository.HasPendingApplicationAsync(request.TourId, guide.Id, cancellationToken).ConfigureAwait(false);
            if (hasPending)
            {
                return Result<Guid>.Failure(new Error("GuideApplication.DuplicatePending", "You already have a pending application for this tour."), Outcome.Conflict);
            }

            var applicationResult = Domain.Entities.GuideApplication.Create(
                request.TourId,
                guide.Id,
                userId,
                request.Message,
                request.RelevantExperience,
                request.ProposedBasePrice);

            if (!applicationResult.IsSuccess)
            {
                return Result<Guid>.Failure(applicationResult.Error!, applicationResult.Outcome);
            }

            await applicationRepository.AddAsync(applicationResult.Value!, cancellationToken).ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<Guid>.Failure(new Error("GuideApplication.ConcurrencyConflict", "Concurrent update detected."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourApplications(request.TourId), cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForGuideApplications(guide.Id), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Guide {GuideId} applied for tour {TourId}", guide.Id, request.TourId);
            return Result<Guid>.Success(applicationResult.Value!.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<Guid>.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
