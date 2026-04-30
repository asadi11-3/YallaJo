using ContentPlaces.Contracts.Places;
using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;
using TourEntity = ContentTours.Domain.Entities.Tour;

namespace ContentTours.Application.Commands.Tour.CreateTour;

public sealed class CreateTourCommandHandler(
    ITourRepository tourRepository,
    IPlaceExistenceService placeExistenceService,
    IContentToursEventUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateTourCommandHandler> logger)
    : ICommandHandler<CreateTourCommand, CreateTourResult>
{
    // Jordan bounding box (warning-only, never blocks creation).
    private const decimal JordanMinLat = 29.18m;
    private const decimal JordanMaxLat = 33.38m;
    private const decimal JordanMinLng = 34.95m;
    private const decimal JordanMaxLng = 39.30m;

    public async Task<Result<CreateTourResult>> Handle(
        CreateTourCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result<CreateTourResult>.Failure(
                    Error.Unauthorized("Authentication is required."),
                    Outcome.Unauthorized);
            }

            var slug = (request.Slug ?? string.Empty).Trim().ToLowerInvariant();
 
            if (await tourRepository.IsSlugReservedAsync(slug, excludeTourId: null, cancellationToken)
                .ConfigureAwait(false))
            {
                return Result<CreateTourResult>.Failure(
                    new Error("Tour.SlugConflict", $"Slug '{slug}' is already in use or reserved."),
                    Outcome.Conflict);
            }

            if (request.PlaceId.HasValue)
            {
                var status = await placeExistenceService
                    .GetStatusAsync(request.PlaceId.Value, cancellationToken)
                    .ConfigureAwait(false);
                switch (status)
                {
                    case PlaceExistenceStatus.NotFound:
                        return Result<CreateTourResult>.Failure(
                            new Error("Tour.PlaceNotFound", $"Place '{request.PlaceId.Value}' does not exist."),
                            Outcome.UnprocessableEntity);
                    case PlaceExistenceStatus.Deleted:
                        return Result<CreateTourResult>.Failure(
                            new Error("Tour.PlaceDeleted", $"Place '{request.PlaceId.Value}' has been deleted."),
                            Outcome.UnprocessableEntity);
                }
            }

            if (request.Latitude < JordanMinLat || request.Latitude > JordanMaxLat
                || request.Longitude < JordanMinLng || request.Longitude > JordanMaxLng)
            {
                logger.LogWarning(
                    "Tour location outside Jordan bounding box: ({Latitude}, {Longitude})",
                    request.Latitude, request.Longitude);
            }

            Location? meetingPoint = null;
            if (request.MeetingPointLatitude.HasValue && request.MeetingPointLongitude.HasValue)
            {
                meetingPoint = new Location(
                    request.MeetingPointLatitude.Value,
                    request.MeetingPointLongitude.Value);
            }

            var tour = TourEntity.Create(
                name:                    request.Name,
                slug:                    slug,
                difficulty:              request.Difficulty,
                durationMinutes:         request.DurationMinutes,
                maxGroupSize:            request.MaxGroupSize,
                basePriceAmount:         request.BasePrice,
                currency:                request.Currency,
                location:                new Location(request.Latitude, request.Longitude),
                createdByUserId:         currentUser.UserId.Value,
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

            await tourRepository.AddAsync(tour, cancellationToken).ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<CreateTourResult>.Failure(
                    new Error(
                        "Tour.ConcurrencyConflict",
                        "This tour was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagToursList, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagToursSearch, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Tour created: {TourId} (Slug={Slug}, CreatedBy={UserId})",
                tour.Id, tour.Slug, currentUser.UserId);

            return Result<CreateTourResult>.Created(
                new CreateTourResult(tour.Id, tour.Name, tour.Slug));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<CreateTourResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
