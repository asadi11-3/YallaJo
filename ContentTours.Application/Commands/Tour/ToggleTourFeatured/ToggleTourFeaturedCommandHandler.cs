using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.Tour.ToggleTourFeatured;

public sealed class ToggleTourFeaturedCommandHandler(
    ITourRepository tourRepo,
    IContentToursEventUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ToggleTourFeaturedCommandHandler> logger)
    : ICommandHandler<ToggleTourFeaturedCommand>
{
    public async Task<Result> Handle(ToggleTourFeaturedCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourRepo.GetByIdAsync(request.TourId, cancellationToken);
            if (tour is null || tour.IsDeleted)
            {
                return Result.Failure(
                   new Error("Tour.NotFound", $"Tour '{request.TourId}' was not found."),
                   Outcome.NotFound);
            }


            // Only Approved tours can be featured (IsApproved() centralises PW-1 mapping)
            if (!tour.Status.IsApproved())
            {
                return Result.Fail(
                   Outcome.Conflict,
                   new Error(
                       "Tour.CannotFeatureNonApproved",
                       "Only Approved tours can be featured."));
            }

            var willChange = tour.IsFeatured != request.IsFeatured;

            // SetFeatured is idempotent — no event if value unchanged
            tour.SetFeatured(request.IsFeatured, currentUser.UserId!.Value);

            try
            {
                // dispatches TourFeaturedChangedDomainEvent if changed
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(
                    ex,
                    "Concurrency conflict while {Action}: TourId={TourId}",
                    nameof(ToggleTourFeaturedCommand), request.TourId);

                return Result.Failure(
                    new Error(
                        "Tour.ConcurrencyConflict",
                        "This tour was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            if (willChange)
            {
                await cache.RemoveByTagAsync("tours:featured", cancellationToken);
                await cache.RemoveByTagAsync(TourCacheKeys.TagForTour(tour.Id), cancellationToken);
                await cache.RemoveByTagAsync("tours:list", cancellationToken);
                await cache.RemoveByTagAsync("tours:search", cancellationToken);
                await cache.RemoveByTagAsync($"my-tours:{tour.CreatedByUserId}", cancellationToken);

                logger.LogInformation(
                    "Tour {TourId} featured set to {IsFeatured} by {UserId}",
                    tour.Id, request.IsFeatured, currentUser.UserId);
            }

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
