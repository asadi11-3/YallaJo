using Booking.Application.Caching;
using Booking.Application.Commands.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Application.Commands.UpdateAvailabilitySlot;

public sealed class UpdateAvailabilitySlotCommandHandler(
    IAvailabilitySlotRepository availabilitySlotRepository,
    IBookingTourSnapshotReader tourSnapshotReader,
    IBookingProviderSnapshotReader providerSnapshotReader,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateAvailabilitySlotCommandHandler> logger)
    : ICommandHandler<UpdateAvailabilitySlotCommand, AvailabilitySlotDto>
{
    public async Task<Result<AvailabilitySlotDto>> Handle(
        UpdateAvailabilitySlotCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var slot = await availabilitySlotRepository
                .GetByIdWithLockAsync(request.SlotId, cancellationToken)
                .ConfigureAwait(false);

            if (slot is null || !slot.IsActive || slot.TourId is null)
            {
                return Result.Failure<AvailabilitySlotDto>(
                    new Error("AvailabilitySlot.NotFound", "Availability slot was not found."),
                    Outcome.NotFound);
            }

            var ownership = await AvailabilitySlotOwnership.ResolveAsync(
                slot.TourId.Value,
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

            try
            {
                slot.UpdateCapacity(request.MaxCapacity);
            }
            catch (BusinessRuleViolationException)
            {
                return Result.Failure<AvailabilitySlotDto>(
                    new Error(
                        "AvailabilitySlot.CapacityExceeded",
                        "Capacity cannot be reduced below booked + locked seats."),
                    Outcome.Conflict);
            }

            availabilitySlotRepository.AttachAndMarkModifiedWithConcurrency(
                slot,
                concurrencyPropertyName: nameof(slot.RowVersion),
                originalConcurrencyValue: request.RowVersion);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<AvailabilitySlotDto>(
                    new Error("AvailabilitySlot.StaleRowVersion", "Slot was modified by another caller. Reload and retry."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(BookingAvailabilityCacheKeys.TourDateTag(slot.TourId.Value, slot.Date), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(BookingAvailabilityCacheKeys.TourTag(slot.TourId.Value), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Availability slot {SlotId} updated by {UserId}",
                slot.Id,
                currentUser.UserId);

            return Result.Success(new AvailabilitySlotDto(
                Id: slot.Id,
                TourId: slot.TourId.Value,
                Date: slot.Date,
                StartTime: slot.StartTime,
                EndTime: slot.EndTime,
                MaxCapacity: slot.MaxCapacity,
                AvailableCount: slot.AvailableCount,
                RowVersion: Convert.ToBase64String(slot.RowVersion ?? [])));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<AvailabilitySlotDto>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }
}
