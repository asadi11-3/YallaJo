using ContentTours.Application.Caching;
using ContentTours.Application.Common;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.Tour.ApproveTour;

public sealed class ApproveTourCommandHandler(
    ITourRepository tourRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ApproveTourCommandHandler> logger)
    : ICommandHandler<ApproveTourCommand>
{
    public async Task<Result> Handle(ApproveTourCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure(
                    Error.Unauthorized("Authentication is required."),
                    Outcome.Unauthorized);
            }

            var tour = await tourRepository
                .GetByIdAsync(request.Id, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);
            if (tour is null)
            {
                return Result.Failure(
                    new Error("Tour.NotFound", $"Tour '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            if (tour.Status != Domain.Enums.TourStatus.Pending)
            {
                return Result.Failure(
                    new Error(
                        "Tour.InvalidTransition",
                        $"Cannot approve a tour with status {tour.Status}. Required: Pending."),
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
                tour.Approve(currentUser.UserId.Value);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(
                    new Error("Tour.InvalidTransition", ex.Message),
                    Outcome.Conflict);
            }

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

            logger.LogInformation(
                "Tour approved: {TourId} (CreatedBy={CreatedByUserId}, By={UserId})",
                tour.Id, tour.CreatedByUserId, currentUser.UserId);

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
