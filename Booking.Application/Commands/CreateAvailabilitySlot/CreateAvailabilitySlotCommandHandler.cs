using Booking.Application.Caching;
using Booking.Application.Commands.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.CreateAvailabilitySlot;

public sealed class CreateAvailabilitySlotCommandHandler(
    IAvailabilitySlotRepository availabilitySlotRepository,
    IBookingTourSnapshotReader tourSnapshotReader,
    IBookingProviderSnapshotReader providerSnapshotReader,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateAvailabilitySlotCommandHandler> logger)
    : ICommandHandler<CreateAvailabilitySlotCommand, AvailabilitySlotDto>
{
    public async Task<Result<AvailabilitySlotDto>> Handle(
        CreateAvailabilitySlotCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ownership = await AvailabilitySlotOwnership.ResolveAsync(
                request.TourId,
                currentUser,
                tourSnapshotReader,
                providerSnapshotReader,
                cancellationToken).ConfigureAwait(false);

            if (!ownership.IsSuccess)
            {
                return Result.Failure<AvailabilitySlotDto>(ownership.Errors[0], ownership.Outcome);
            }

            var ownerContext = ownership.Value!;

            if (request.MaxCapacity > ownerContext.TourSnapshot.MaxGroupSize)
            {
                return Result.Failure<AvailabilitySlotDto>(
                    new Error(
                        "AvailabilitySlot.CapacityExceedsTour",
                        $"MaxCapacity cannot exceed the tour group limit ({ownerContext.TourSnapshot.MaxGroupSize})."),
                    Outcome.Invalid);
            }

            var hasOverlap = await availabilitySlotRepository.AnyOverlapAsync(
                request.TourId,
                request.Date,
                request.StartTime,
                request.EndTime,
                excludeId: null,
                cancellationToken).ConfigureAwait(false);

            if (hasOverlap)
            {
                return Result.Failure<AvailabilitySlotDto>(
                    new Error(
                        "AvailabilitySlot.Overlap",
                        "Another active slot overlaps this time range for the same tour/date."),
                    Outcome.Conflict);
            }

            var tourGuideId = await availabilitySlotRepository
                .GetTourGuideIdByUserIdAsync(ownerContext.ProviderSnapshot.OwnerUserId, cancellationToken)
                .ConfigureAwait(false);

            if (tourGuideId is null)
            {
                return Result.Failure<AvailabilitySlotDto>(
                    new Error(
                        AvailabilitySlotOwnership.NotAProviderErrorCode,
                        "Current user is not registered as an active tour guide."),
                    Outcome.Forbidden);
            }

            var slot = AvailabilitySlot.CreateForTour(
                tourGuideId: tourGuideId.Value,
                tourId: request.TourId,
                date: request.Date,
                start: request.StartTime,
                end: request.EndTime,
                maxCapacity: request.MaxCapacity);

            await availabilitySlotRepository.AddAsync(slot, cancellationToken).ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<AvailabilitySlotDto>(
                    new Error("AvailabilitySlot.CapacityConflict", "Slot was modified by another booking."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(BookingAvailabilityCacheKeys.TourTag(request.TourId), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(BookingAvailabilityCacheKeys.TourDateTag(request.TourId, request.Date), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Availability slot {SlotId} created by {UserId}",
                slot.Id,
                currentUser.UserId);

            var dto = new AvailabilitySlotDto(
                Id: slot.Id,
                TourId: request.TourId,
                Date: slot.Date,
                StartTime: slot.StartTime,
                EndTime: slot.EndTime,
                MaxCapacity: slot.MaxCapacity,
                AvailableCount: slot.AvailableCount,
                RowVersion: Convert.ToBase64String(slot.RowVersion ?? []));

            return Result.Created(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<AvailabilitySlotDto>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }
}
