using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.DeactivateAvailabilitySlot;

public sealed class DeactivateAvailabilitySlotCommandHandler(
    IAvailabilitySlotRepository slotRepository,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<DeactivateAvailabilitySlotCommandHandler> logger)
    : ICommandHandler<DeactivateAvailabilitySlotCommand>
{
    public async Task<Result> Handle(DeactivateAvailabilitySlotCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var guideUserId = currentUser.UserId!.Value;

            var slot = await slotRepository
                .GetByIdAsync(request.SlotId, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);

            if (slot is null)
            {
                return Result.Failure(new Error("AvailabilitySlot.NotFound", "Slot not found."), Outcome.NotFound);
            }

            if (slot.TourGuideId != guideUserId)
            {
                return Result.Failure(new Error("AvailabilitySlot.Unauthorized", "You do not own this slot."), Outcome.Forbidden);
            }

            slot.Deactivate();
            slotRepository.Update(slot);
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

            if (slot.TourId.HasValue)
            {
                await cache.RemoveByTagAsync($"availability:tour:{slot.TourId:D}", cancellationToken).ConfigureAwait(false);
            }

            logger.LogInformation("AvailabilitySlot {SlotId} deactivated by guide {GuideUserId}.", slot.Id, guideUserId);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Request was cancelled."), Outcome.Canceled);
        }
    }
}
