using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Common;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.Tour.ReinstateTour;

public sealed class ReinstateTourCommandHandler(
    ITourRepository tourRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<ReinstateTourCommandHandler> logger)
    : ICommandHandler<ReinstateTourCommand>
{
    public async Task<Result> Handle(ReinstateTourCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourRepository
                .GetByIdAsync(request.Id, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);
            if (tour is null)
            {
                return Result.Failure(
                    new Error("Tour.NotFound", $"Tour '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            if (tour.Status != Domain.Enums.TourStatus.Suspended)
            {
                return Result.Failure(
                    new Error(
                        "Tour.InvalidTransition",
                        $"Cannot reinstate a tour with status {tour.Status}. Required: Suspended."),
                    Outcome.Conflict);
            }

            if (!RowVersionUtil.Equal(tour.RowVersion, request.RowVersion))
            {
                return Result.Failure(
                    new Error(
                        "Tour.ConcurrencyConflict",
                        "This tour was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            try
            {
                tour.Reinstate();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(
                    new Error("Tour.InvalidTransition", ex.Message),
                    Outcome.Conflict);
            }

            var wasFeatured = tour.IsFeatured;

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Tour.ConcurrencyConflict",
                        "This tour was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTour(tour.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagToursList, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagToursSearch, cancellationToken)
                .ConfigureAwait(false);
            if (wasFeatured)
            {
                await cache.RemoveByTagAsync(ContentToursCacheKeys.TagToursFeatured, cancellationToken)
                    .ConfigureAwait(false);
            }

            logger.LogInformation(
                "Tour reinstated: {TourId} (CreatedBy={CreatedByUserId})",
                tour.Id, tour.CreatedByUserId);

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
