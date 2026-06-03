using ContentCore.Contracts.Attachments;
using ContentPlaces.Contracts.Places;
using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Common;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.Tour.SubmitTour;

public sealed class SubmitTourCommandHandler(
    ITourRepository tourRepository,
    IAttachmentExistenceService attachmentExistenceService,
    IPlaceExistenceService placeExistenceService,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<SubmitTourCommandHandler> logger)
    : ICommandHandler<SubmitTourCommand>
{
    private const int MinDescriptionLength = 100;

    // Stable Contracts-level entity-type string. Matches the
    // ContentCore.Domain.Enums.EntityType.Tour member name (case-insensitive parse on
    // the implementation side). Keeps the cross-module call enum-free.
    private const string TourEntityType = "Tour";

    public async Task<Result> Handle(SubmitTourCommand request, CancellationToken cancellationToken)
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
                    new Error("Tour.NotOwner", "You do not have permission to submit this tour."),
                    Outcome.Forbidden);
            }

            if (tour.Status != Domain.Enums.TourStatus.Draft)
            {
                return Result.Failure(
                    new Error(
                        "Tour.InvalidTransition",
                        $"Cannot submit a tour with status {tour.Status}. Required: Draft."),
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

            // ── Aggregated pre-submit validation gate ─────────────────────────
            var errors = new List<Error>();

            // 3. At least 1 image attachment.
            var hasImage = await attachmentExistenceService
                .HasEntityImageAsync(TourEntityType, tour.Id, cancellationToken)
                .ConfigureAwait(false);
            if (!hasImage)
            {
                errors.Add(new Error("Tour.NoImages", "Tour must have at least one image."));
            }

            // 4. At least 1 active TourPricingTier.
            var hasActivePricing = await tourRepository
                .HasActivePricingAsync(tour.Id, cancellationToken)
                .ConfigureAwait(false);
            if (!hasActivePricing)
            {
                errors.Add(new Error("Tour.NoPricing", "Tour must have at least one active pricing tier."));
            }

            // 5. At least 1 active TourSchedule.
            var hasActiveSchedule = await tourRepository
                .HasActiveScheduleAsync(tour.Id, cancellationToken)
                .ConfigureAwait(false);
            if (!hasActiveSchedule)
            {
                errors.Add(new Error("Tour.NoSchedule", "Tour must have at least one active schedule."));
            }

            // 6. Description present and >= 100 chars.
            if (string.IsNullOrWhiteSpace(tour.Description) || tour.Description.Trim().Length < MinDescriptionLength)
            {
                errors.Add(new Error(
                    "Tour.DescriptionTooShort",
                    $"Description is required and must be at least {MinDescriptionLength} characters."));
            }

            // 7. MeetingPoint exists with lat/lng.
            if (tour.MeetingPoint is null)
            {
                errors.Add(new Error(
                    "Tour.MissingMeetingPoint",
                    "MeetingPoint coordinates are required to submit."));
            }

            // 8. PlaceId still references a non-deleted Place (always required now).
            if (tour.PlaceId != Guid.Empty)
            {
                var status = await placeExistenceService
                    .GetStatusAsync(tour.PlaceId, cancellationToken)
                    .ConfigureAwait(false);
                switch (status)
                {
                    case PlaceExistenceStatus.NotFound:
                        errors.Add(new Error(
                            "Tour.PlaceNotFound",
                            $"Linked Place '{tour.PlaceId}' no longer exists."));
                        break;
                    case PlaceExistenceStatus.Deleted:
                        errors.Add(new Error(
                            "Tour.PlaceDeleted",
                            $"Linked Place '{tour.PlaceId}' has been deleted."));
                        break;
                }
            }

            // 9. At least 1 active Adult pricing tier.
            var hasAdult = await tourRepository
                .HasActiveAdultPricingAsync(tour.Id, cancellationToken)
                .ConfigureAwait(false);
            if (!hasAdult)
            {
                errors.Add(new Error(
                    "Tour.NoAdultPricingTier",
                    "Tour must have at least one active Adult pricing tier."));
            }

            if (errors.Count > 0)
            {
                logger.LogInformation(
                    "Tour {TourId} submit blocked by {ErrorCount} validation failure(s).",
                    tour.Id, errors.Count);

                // Return all errors under one umbrella outcome (422).
                var umbrella = new Error(
                    "Tour.SubmitValidationFailed",
                    "Tour cannot be submitted; one or more pre-submit checks failed.");
                var combined = new List<Error> { umbrella };
                combined.AddRange(errors);
                return Result.Fail(Outcome.UnprocessableEntity, combined.ToArray());
            }

            // All gates passed — let the aggregate transition state and raise events.
            try
            {
                tour.Submit();
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
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForMyTours(tour.CreatedByUserId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Tour submitted: {TourId} (CreatedBy={CreatedByUserId}, By={UserId})",
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
