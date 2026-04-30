using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.Tour.SuspendTour;

public sealed class SuspendTourCommandHandler(
    ITourRepository tourRepository,
    IContentToursEventUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<SuspendTourCommandHandler> logger)
    : ICommandHandler<SuspendTourCommand>
{
    public async Task<Result> Handle(SuspendTourCommand request, CancellationToken cancellationToken)
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

            if (tour.Status != Domain.Enums.TourStatus.Approved)
            {
                return Result.Failure(
                    new Error(
                        "Tour.InvalidTransition",
                        $"Cannot suspend a tour with status {tour.Status}. Required: Approved."),
                    Outcome.Conflict);
            }

            if (!RowVersionsEqual(tour.RowVersion, request.RowVersion))
            {
                return Result.Failure(
                    new Error(
                        "Tour.ConcurrencyConflict",
                        "This tour was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            try
            {
                tour.Suspend(request.Reason);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(
                    new Error("Tour.InvalidTransition", ex.Message),
                    Outcome.Conflict);
            }
            catch (ArgumentException)
            {
                return Result.Failure(
                    new Error("Tour.ReasonRequired", "A suspension reason is required."),
                    Outcome.Invalid);
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
                "Tour suspended: {TourId} (CreatedBy={CreatedByUserId})",
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

    private static bool RowVersionsEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length) return false;
        for (var i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i]) return false;
        }

        return true;
    }
}
