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

namespace Booking.Application.Commands.DeleteAvailabilitySlot;

public sealed class DeleteAvailabilitySlotCommandHandler(
    IAvailabilitySlotRepository availabilitySlotRepository,
    IBookingTourSnapshotReader tourSnapshotReader,
    IBookingProviderSnapshotReader providerSnapshotReader,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<DeleteAvailabilitySlotCommandHandler> logger)
    : ICommandHandler<DeleteAvailabilitySlotCommand>
{
    public async Task<Result> Handle(DeleteAvailabilitySlotCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var slot = await availabilitySlotRepository
                .GetByIdWithLockAsync(request.SlotId, cancellationToken)
                .ConfigureAwait(false);

            if (slot is null || !slot.IsActive || slot.TourId is null)
            {
                return Result.Failure(
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
                return Result.Failure(ownership.Errors[0], ownership.Outcome);
            }

            try
            {
                slot.RequestDeactivation();
            }
            catch (BusinessRuleViolationException)
            {
                return Result.Failure(
                    new Error("AvailabilitySlot.HasBookings", "Cannot delete a slot that already has bookings."),
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
                return Result.Failure(
                    new Error("AvailabilitySlot.StaleRowVersion", "Slot was modified by another caller. Reload and retry."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(BookingAvailabilityCacheKeys.TourDateTag(slot.TourId.Value, slot.Date), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(BookingAvailabilityCacheKeys.TourTag(slot.TourId.Value), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Availability slot {SlotId} deleted by {UserId}",
                slot.Id,
                currentUser.UserId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }
}
