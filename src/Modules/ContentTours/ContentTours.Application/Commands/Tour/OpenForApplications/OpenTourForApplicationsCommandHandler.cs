using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.Tour.OpenForApplications;

public sealed class OpenTourForApplicationsCommandHandler(
    ITourRepository tourRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<OpenTourForApplicationsCommandHandler> logger)
    : ICommandHandler<OpenTourForApplicationsCommand>
{
    public async Task<Result> Handle(OpenTourForApplicationsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourRepository.GetByIdAsync(request.TourId, cancellationToken, asNoTracking: false).ConfigureAwait(false);
            if (tour is null || tour.IsDeleted)
            {
                return Result.Failure(new Error("Tour.NotFound", $"Tour '{request.TourId}' not found."), Outcome.NotFound);
            }

            if (tour.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result.Failure(new Error("Tour.NotOwner", "You do not own this tour."), Outcome.Forbidden);
            }

            tour.OpenForApplications();

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("Tour.ConcurrencyConflict", "Concurrent update detected."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTour(tour.Id), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Tour {TourId} opened for applications by {UserId}", tour.Id, currentUser.UserId);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
