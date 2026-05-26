using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.CreateAvailabilitySlot;

public sealed class CreateAvailabilitySlotCommandHandler(
    IAvailabilitySlotRepository slotRepository,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateAvailabilitySlotCommandHandler> logger)
    : ICommandHandler<CreateAvailabilitySlotCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateAvailabilitySlotCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var guideUserId = currentUser.UserId!.Value;

            if (request.Date < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                return Result.Failure<Guid>(
                    new Error("AvailabilitySlot.PastDate", "Cannot create availability slots in the past."),
                    Outcome.Invalid);
            }

            var slot = AvailabilitySlot.CreateForTour(
                guideUserId,
                request.TourId,
                request.Date,
                request.StartTime,
                request.EndTime,
                request.MaxCapacity);

            slotRepository.Add(slot);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cache.RemoveByTagAsync($"availability:tour:{request.TourId:D}", cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "AvailabilitySlot {SlotId} created for tour {TourId} by guide {GuideUserId}.",
                slot.Id, request.TourId, guideUserId);

            return Result.Success(slot.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<Guid>(new Error("Request.Cancelled", "Request was cancelled."), Outcome.Canceled);
        }
    }
}
