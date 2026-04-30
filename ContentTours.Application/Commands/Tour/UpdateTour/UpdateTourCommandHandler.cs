using ContentPlaces.Contracts.Places;
using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Application.Commands.Tour.UpdateTour;

public sealed class UpdateTourCommandHandler(
    ITourRepository tourRepository,
    IPlaceExistenceService placeExistenceService,
    IContentToursEventUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateTourCommandHandler> logger)
    : ICommandHandler<UpdateTourCommand>
{
    private const decimal JordanMinLat = 29.18m;
    private const decimal JordanMaxLat = 33.38m;
    private const decimal JordanMinLng = 34.95m;
    private const decimal JordanMaxLng = 39.30m;

    public async Task<Result> Handle(UpdateTourCommand request, CancellationToken cancellationToken)
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

            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;
            if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId.Value)
            {
                return Result.Failure(
                    new Error("Tour.NotOwner", "You do not have permission to update this tour."),
                    Outcome.Forbidden);
            }

            if (!RowVersionsEqual(tour.RowVersion, request.RowVersion))
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

            if (request.PlaceId.HasValue)
            {
                var status = await placeExistenceService
                    .GetStatusAsync(request.PlaceId.Value, cancellationToken)
                    .ConfigureAwait(false);
                switch (status)
                {
                    case PlaceExistenceStatus.NotFound:
                        return Result.Failure(
                            new Error("Tour.PlaceNotFound", $"Place '{request.PlaceId.Value}' does not exist."),
                            Outcome.UnprocessableEntity);
                    case PlaceExistenceStatus.Deleted:
                        return Result.Failure(
                            new Error("Tour.PlaceDeleted", $"Place '{request.PlaceId.Value}' has been deleted."),
                            Outcome.UnprocessableEntity);
                }
            }

            if (request.Latitude < JordanMinLat || request.Latitude > JordanMaxLat
                || request.Longitude < JordanMinLng || request.Longitude > JordanMaxLng)
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
