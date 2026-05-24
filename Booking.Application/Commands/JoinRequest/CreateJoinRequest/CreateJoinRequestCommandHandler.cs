using Booking.Application.Interfaces;
using Booking.Application.Queries.JoinRequest;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Application.Commands.JoinRequest.CreateJoinRequest;

public sealed class CreateJoinRequestCommandHandler(
    IJoinRequestRepository joinRequestRepository,
    ITourBookingRepository tourBookingRepository,
    IAvailabilitySlotRepository availabilitySlotRepository,
    IBookingUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<CreateJoinRequestCommandHandler> logger)
    : ICommandHandler<CreateJoinRequestCommand, JoinRequestDto>
{
    public async Task<Result<JoinRequestDto>> Handle(CreateJoinRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.Unauthorized", "Authentication is required to submit a join request."),
                    Outcome.Unauthorized);
            }

            var requesterId = currentUser.UserId.Value;

            if (request.ParticipantCount < 1)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.InvalidParticipantCount", "ParticipantCount must be at least 1."),
                    Outcome.Invalid);
            }

            var booking = await tourBookingRepository
                .GetByIdAsync(request.BookingId, cancellationToken)
                .ConfigureAwait(false);
            if (booking is null)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.BookingNotFound", "Target booking was not found."),
                    Outcome.NotFound);
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.BookingNotConfirmed", $"Join requests are only allowed on Confirmed bookings (current state: {booking.Status})."),
                    Outcome.Invalid);
            }

            if (booking.UserId == requesterId)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.SelfJoin", "You cannot create a join request on your own booking."),
                    Outcome.Invalid);
            }

            var hasPending = await joinRequestRepository
                .ExistsPendingForUserAndBookingAsync(booking.Id, requesterId, cancellationToken)
                .ConfigureAwait(false);
            if (hasPending)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.Duplicate", "You already have a pending join request for this booking."),
                    Outcome.Conflict);
            }

            var slot = await availabilitySlotRepository
                .GetByIdAsync(booking.AvailabilitySlotId, cancellationToken)
                .ConfigureAwait(false);
            if (slot is null)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.SlotNotFound", "Availability slot for the target booking was not found."),
                    Outcome.NotFound);
            }

            if (slot.AvailableCount < request.ParticipantCount)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.CapacityFull", $"Insufficient capacity: requested {request.ParticipantCount}, available {slot.AvailableCount}."),
                    Outcome.Conflict);
            }

            Booking.Domain.Entities.JoinRequest joinRequest;
            try
            {
                joinRequest = Booking.Domain.Entities.JoinRequest.Create(
                    tourBookingId: booking.Id,
                    userId: requesterId,
                    participantCount: request.ParticipantCount,
                    message: request.Message);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Domain rejected join request creation for booking {BookingId} by user {UserId}.", request.BookingId, requesterId);
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.InvalidParticipantCount", ex.Message),
                    Outcome.Invalid);
            }

            await joinRequestRepository.AddAsync(joinRequest, cancellationToken).ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "DbUpdateException creating join request for booking {BookingId} by user {UserId}; treated as duplicate-pending race.", request.BookingId, requesterId);
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.Duplicate", "A pending join request for this booking already exists. Please retry."),
                    Outcome.Conflict);
            }

            logger.LogInformation(
                "JoinRequest {JoinRequestId} created for booking {BookingId} by user {UserId} (participants={Participants}).",
                joinRequest.Id, booking.Id, requesterId, request.ParticipantCount);

            return Result.Created(JoinRequestMapper.ToDto(joinRequest));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<JoinRequestDto>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }
}
