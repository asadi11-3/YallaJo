using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.SubmitJoinRequest;

public sealed class SubmitJoinRequestCommandHandler(
    ITourBookingRepository tourBookingRepository,
    IAvailabilitySlotRepository slotRepository,
    IJoinRequestRepository joinRequestRepository,
    IBookingUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<SubmitJoinRequestCommandHandler> logger)
    : ICommandHandler<SubmitJoinRequestCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SubmitJoinRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUser.UserId!.Value;
            var utcNow = DateTime.UtcNow;

            // Validate parent booking exists and is confirmed
            var booking = await tourBookingRepository
                .GetByIdAsync(request.TourBookingId, cancellationToken)
                .ConfigureAwait(false);

            if (booking is null)
            {
                return Result.Failure<Guid>(
                    new Error("TourBooking.NotFound", "Booking not found."),
                    Outcome.NotFound);
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                return Result.Failure<Guid>(
                    new Error("TourBooking.NotConfirmed", "You can only join a confirmed booking."),
                    Outcome.Invalid);
            }

            // Prevent joining own booking
            if (booking.UserId == userId)
            {
                return Result.Failure<Guid>(
                    new Error("JoinRequest.OwnBooking", "You cannot join your own booking."),
                    Outcome.Invalid);
            }

            // Check for existing pending request
            var hasPending = await joinRequestRepository
                .HasPendingRequestAsync(request.TourBookingId, userId, cancellationToken)
                .ConfigureAwait(false);

            if (hasPending)
            {
                return Result.Failure<Guid>(
                    new Error("JoinRequest.AlreadyPending", "You already have a pending join request for this booking."),
                    Outcome.Conflict);
            }

            // Validate slot capacity
            var slot = await slotRepository
                .GetByIdAsync(request.AvailabilitySlotId, cancellationToken)
                .ConfigureAwait(false);

            if (slot is null || !slot.IsActive)
            {
                return Result.Failure<Guid>(
                    new Error("AvailabilitySlot.NotFound", "Availability slot not found."),
                    Outcome.NotFound);
            }

            var remaining = slot.MaxCapacity - slot.BookedCount - slot.LockedCount;
            if (request.ParticipantCount > remaining)
            {
                return Result.Failure<Guid>(
                    new Error("AvailabilitySlot.InsufficientCapacity", $"Only {remaining} spots remain."),
                    Outcome.Conflict);
            }

            var joinRequestResult = JoinRequest.Create(
                request.TourBookingId,
                request.AvailabilitySlotId,
                userId,
                request.ParticipantCount,
                request.Message,
                utcNow);

            if (!joinRequestResult.IsSuccess)
            {
                return Result.Failure<Guid>(joinRequestResult.Error);
            }

            joinRequestRepository.Add(joinRequestResult.Value!);

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "JoinRequest {JoinRequestId} submitted by user {UserId} for booking {BookingId}.",
                joinRequestResult.Value!.Id, userId, request.TourBookingId);

            return Result.Success(joinRequestResult.Value!.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<Guid>(new Error("Request.Cancelled", "Request was cancelled."), Outcome.Canceled);
        }
    }
}
