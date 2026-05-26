using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.UpdateAvailabilitySlot;

public sealed class UpdateAvailabilitySlotCommandHandler(
    IAvailabilitySlotRepository slotRepository,
    IBookingUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<UpdateAvailabilitySlotCommandHandler> logger)
    : ICommandHandler<UpdateAvailabilitySlotCommand>
{
    public async Task<Result> Handle(UpdateAvailabilitySlotCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var slot = await slotRepository.GetByIdAsync(request.SlotId, cancellationToken, asNoTracking: false).ConfigureAwait(false);
            if (slot is null)
            {
                return Result.Failure(new Error("AvailabilitySlot.NotFound", "Availability slot not found."), Outcome.NotFound);
            }

            // Ownership check — only the guide who owns the slot can modify it
            var guideUserId = currentUser.UserId!.Value;
            if (slot.TourGuideId != guideUserId)
            {
                return Result.Failure(new Error("AvailabilitySlot.Unauthorized", "You do not own this slot."), Outcome.Forbidden);
            }

            if (request.Date < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                return Result.Failure(new Error("AvailabilitySlot.PastDate", "Cannot set slot date in the past."), Outcome.Invalid);
            }

            try
            {
                slot.Update(
                    request.Date,
                    request.StartTime,
                    request.EndTime,
                    request.MaxCapacity,
                    request.PriceOverride,
                    request.PriceOverrideCurrency);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Result.Failure(new Error("AvailabilitySlot.InvalidUpdate", ex.Message), Outcome.Invalid);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("AvailabilitySlot {SlotId} updated by guide {GuideUserId}", slot.Id, guideUserId);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
