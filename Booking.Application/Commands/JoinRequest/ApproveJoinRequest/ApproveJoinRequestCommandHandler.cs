using Booking.Application.Interfaces;
using Booking.Application.Queries.JoinRequest;
using Booking.Contracts.Authorization;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Application.Commands.JoinRequest.ApproveJoinRequest;

public sealed class ApproveJoinRequestCommandHandler(
    IJoinRequestRepository joinRequestRepository,
    ITourBookingRepository tourBookingRepository,
    IAvailabilitySlotRepository availabilitySlotRepository,
    IBookingUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<ApproveJoinRequestCommandHandler> logger)
    : ICommandHandler<ApproveJoinRequestCommand, JoinRequestDto>
{
    public async Task<Result<JoinRequestDto>> Handle(ApproveJoinRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            var viewerId = currentUser.UserId.Value;

            var joinRequest = await joinRequestRepository
                .GetByIdTrackedAsync(request.JoinRequestId, cancellationToken)
                .ConfigureAwait(false);
            if (joinRequest is null)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.NotFound", "Join request was not found."),
                    Outcome.NotFound);
            }

            if (joinRequest.Status != JoinRequestStatus.Pending)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.NotPending", $"Cannot approve a join request in state {joinRequest.Status}."),
                    Outcome.Invalid);
            }

            var booking = await tourBookingRepository
                .GetByIdAsync(joinRequest.TourBookingId, cancellationToken)
                .ConfigureAwait(false);
            if (booking is null)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.BookingNotFound", "Target booking was not found."),
                    Outcome.NotFound);
            }

            var isOwner = booking.UserId == viewerId;
            var isAdmin = currentUser.HasPermission($"{BookingFeatures.AdminBookingDashboard}.{AppAction.Update}");
            if (!isOwner && !isAdmin)
            {
                logger.LogWarning(
                    "Approve forbidden: viewer {ViewerId} is neither booking owner nor admin for join request {JoinRequestId}.",
                    viewerId, joinRequest.Id);
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.OwnerMismatch", "You are not authorized to approve this join request."),
                    Outcome.Forbidden);
            }

            // Re-load slot WITH RowVersion so EF emits the optimistic-concurrency WHERE clause on save.
            var slot = await availabilitySlotRepository
                .GetByIdWithLockAsync(booking.AvailabilitySlotId, cancellationToken)
                .ConfigureAwait(false);
            if (slot is null)
            {
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.SlotNotFound", "Availability slot for the target booking was not found."),
                    Outcome.NotFound);
            }

            try
            {
                slot.Book(joinRequest.ParticipantCount);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Capacity rejected approval of join request {JoinRequestId}.", joinRequest.Id);
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.CapacityFull", ex.Message),
                    Outcome.Conflict);
            }

            try
            {
                joinRequest.Approve();
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Domain rejected approve for join request {JoinRequestId}.", joinRequest.Id);
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.NotPending", ex.Message),
                    Outcome.Invalid);
            }

            joinRequestRepository.Update(joinRequest);
            availabilitySlotRepository.Update(slot);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict approving join request {JoinRequestId}.", joinRequest.Id);
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.ConcurrencyConflict", "Join request or slot was modified by another request. Please retry."),
                    Outcome.Conflict);
            }

            logger.LogInformation(
                "JoinRequest {JoinRequestId} approved for booking {BookingId} by user {ViewerId} (admin={IsAdmin}, participants={Participants}).",
                joinRequest.Id, booking.Id, viewerId, isAdmin, joinRequest.ParticipantCount);

            return Result.Success(JoinRequestMapper.ToDto(joinRequest));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<JoinRequestDto>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }
}
