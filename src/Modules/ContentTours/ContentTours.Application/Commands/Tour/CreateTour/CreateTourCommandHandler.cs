using Accounts.Contracts.Abstractions;
using ContentPlaces.Contracts.Places;
using ContentTours.Domain.Enums;
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
using YallaJo.SharedKernel.Domain.ValueObjects;
using TourEntity = ContentTours.Domain.Entities.Tour;

namespace ContentTours.Application.Commands.Tour.CreateTour;

public sealed class CreateTourCommandHandler(
    ITourRepository tourRepository,
    IPlaceExistenceService placeExistenceService,
    IProviderStatusService providerStatusService,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateTourCommandHandler> logger)
    : ICommandHandler<CreateTourCommand, CreateTourResult>
{
    private const int MaxActiveToursPerProvider = 50;

    public async Task<Result<CreateTourResult>> Handle(
        CreateTourCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUser.UserId!.Value;

            if (!await providerStatusService.IsApprovedProviderAsync(userId, cancellationToken)
                .ConfigureAwait(false))
            {
                return Result<CreateTourResult>.Failure(
                    new Error("Provider.NotApproved", "Only approved providers can create tours."),
                    Outcome.Forbidden);
            }

            var activeTourCount = await tourRepository.CountAsync(
                t => t.CreatedByUserId == userId
                    && t.Status != TourStatus.Rejected
                    && t.Status != TourStatus.Suspended
                    && t.Status != TourStatus.Archived,
                cancellationToken).ConfigureAwait(false);

            if (activeTourCount >= MaxActiveToursPerProvider)
            {
                return Result<CreateTourResult>.Failure(
                    new Error("Tour.MaxActiveReached", $"You cannot have more than {MaxActiveToursPerProvider} active tours."),
                    Outcome.Conflict);
            }

            var slug = (request.Slug ?? string.Empty).Trim().ToLowerInvariant();
            if (await tourRepository.IsSlugReservedAsync(slug, excludeTourId: null, cancellationToken)
                .ConfigureAwait(false))
            {
                return Result<CreateTourResult>.Failure(
                    new Error("Tour.SlugConflict", $"Slug '{slug}' is already in use or reserved."),
                    Outcome.Conflict);
            }

            if (await tourRepository.IsNameTakenByProviderAsync(request.Name, userId, excludeTourId: null, cancellationToken)
                .ConfigureAwait(false))
            {
                return Result<CreateTourResult>.Failure(
                    new Error("Tour.NameConflict", $"You already have a tour named '{request.Name}'."),
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
                        return Result<CreateTourResult>.Failure(
                            new Error("Tour.PlaceNotFound", $"Place '{request.PlaceId}' does not exist."),
                            Outcome.UnprocessableEntity);
                    case PlaceExistenceStatus.Deleted:
                        return Result<CreateTourResult>.Failure(
                            new Error("Tour.PlaceDeleted", $"Place '{request.PlaceId}' has been deleted."),
                            Outcome.UnprocessableEntity);
                }
            }

            if (request.Latitude < JordanBounds.MinLat || request.Latitude > JordanBounds.MaxLat
                || request.Longitude < JordanBounds.MinLng || request.Longitude > JordanBounds.MaxLng)
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
                createdByUserId:         userId,
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
