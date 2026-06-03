using ContentPlaces.Contracts.Places;
using ContentTours.Application.Caching;
using ContentTours.Application.Common;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Common;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Application.Commands.Tour.UpdateTour;

public sealed class UpdateTourCommandHandler(
    ITourRepository tourRepository,
    IPlaceExistenceService placeExistenceService,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateTourCommandHandler> logger)
    : ICommandHandler<UpdateTourCommand>
{
    public async Task<Result> Handle(UpdateTourCommand request, CancellationToken cancellationToken)
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

            if (tour.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                    new Error("Tour.NotOwner", "You do not have permission to update this tour."),
                    Outcome.Forbidden);
            }

            if (!RowVersionUtil.Equal(tour.RowVersion, request.RowVersion))
            {
                return Result.Failure(
                    new Error(
                        "Tour.ConcurrencyConflict",
                        "This tour was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            var slug = (request.Slug ?? string.Empty).Trim().ToLowerInvariant();

            if (await tourRepository
                .IsSlugReservedAsync(slug, excludeTourId: tour.Id, cancellationToken)
                .ConfigureAwait(false))
            {
                return Result.Failure(
                    new Error("Tour.SlugConflict", $"Slug '{slug}' is already in use or reserved."),
                    Outcome.Conflict);
            }

            if (await tourRepository.IsNameTakenByProviderAsync(request.Name, tour.CreatedByUserId, excludeTourId: tour.Id, cancellationToken)
                .ConfigureAwait(false))
            {
                return Result.Failure(
                    new Error("Tour.NameConflict", $"A tour named '{request.Name}' already exists for this provider."),
                    Outcome.Conflict);
            }

            if (request.PlaceId != Guid.Empty)
            {
                var status = await placeExistenceService
                    .GetStatusAsync(request.PlaceId, cancellationToken)
                    .ConfigureAwait(false);
                switch (status)
                {
                    case PlaceExistenceStatus.NotFound:
                        return Result.Failure(
                            new Error("Tour.PlaceNotFound", $"Place '{request.PlaceId}' does not exist."),
                            Outcome.UnprocessableEntity);
                    case PlaceExistenceStatus.Deleted:
                        return Result.Failure(
                            new Error("Tour.PlaceDeleted", $"Place '{request.PlaceId}' has been deleted."),
                            Outcome.UnprocessableEntity);
                }
            }

            if (request.Latitude < JordanBounds.MinLat || request.Latitude > JordanBounds.MaxLat
                || request.Longitude < JordanBounds.MinLng || request.Longitude > JordanBounds.MaxLng)
            {
                logger.LogWarning(
                    "Tour {TourId} updated to a location outside Jordan bounding box: ({Latitude}, {Longitude})",
                    tour.Id, request.Latitude, request.Longitude);
            }

            Location? meetingPoint = null;
            if (request.MeetingPointLatitude.HasValue && request.MeetingPointLongitude.HasValue)
            {
                meetingPoint = new Location(
                    request.MeetingPointLatitude.Value,
                    request.MeetingPointLongitude.Value);
            }

            // Capture old slug BEFORE Tour.Update mutates it so we can invalidate
            // the slug-keyed cache entry for the old slug after a successful save.
            var oldSlug = tour.Slug;

            try
            {
                tour.Update(
                    name:                    request.Name,
                    slug:                    slug,
                    difficulty:              request.Difficulty,
                    durationMinutes:         request.DurationMinutes,
                    maxGroupSize:            request.MaxGroupSize,
                    basePriceAmount:         request.BasePrice,
                    currency:                request.Currency,
                    location:                new Location(request.Latitude, request.Longitude),
                    description:             request.Description,
                    shortDescription:        request.ShortDescription,
                    minAge:                  request.MinAge,
                    meetingPoint:            meetingPoint,
                    placeId:                 request.PlaceId,
                    isChildFriendly:         request.IsChildFriendly,
                    isAccessible:            request.IsAccessible,
                    ageRestriction:          request.AgeRestriction,
                    isInstantBooking:        request.IsInstantBooking,
                    cancellationPolicyHours: request.CancellationPolicyHours,
                    metaTitle:               request.MetaTitle,
                    metaDescription:         request.MetaDescription);
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
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForMyTours(tour.CreatedByUserId), cancellationToken)
                .ConfigureAwait(false);

            // P1-005: invalidate slug-keyed cache entries (GetTourBySlugQuery) for both
            // the old and the new slug so a slug change cannot serve stale rows.  When
            // the slug is unchanged we evict only once.
            var newSlug = tour.Slug;
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTourSlug(oldSlug), cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(oldSlug, newSlug, StringComparison.Ordinal))
            {
                await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTourSlug(newSlug), cancellationToken)
                    .ConfigureAwait(false);
            }

            logger.LogInformation(
                "Tour updated: {TourId} (Slug={Slug}, By={UserId})",
                tour.Id, tour.Slug, currentUser.UserId);

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
