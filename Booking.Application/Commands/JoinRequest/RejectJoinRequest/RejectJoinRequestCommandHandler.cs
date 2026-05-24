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

namespace Booking.Application.Commands.JoinRequest.RejectJoinRequest;

public sealed class RejectJoinRequestCommandHandler(
    IJoinRequestRepository joinRequestRepository,
    ITourBookingRepository tourBookingRepository,
    IBookingUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<RejectJoinRequestCommandHandler> logger)
    : ICommandHandler<RejectJoinRequestCommand, JoinRequestDto>
{
    public async Task<Result<JoinRequestDto>> Handle(RejectJoinRequestCommand request, CancellationToken cancellationToken)
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
                    new Error("JoinRequest.NotPending", $"Cannot reject a join request in state {joinRequest.Status}."),
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
                    "Reject forbidden: viewer {ViewerId} is neither booking owner nor admin for join request {JoinRequestId}.",
                    viewerId, joinRequest.Id);
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.OwnerMismatch", "You are not authorized to reject this join request."),
                    Outcome.Forbidden);
            }

            try
            {
                joinRequest.Reject(request.Reason);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Domain rejected reject for join request {JoinRequestId}.", joinRequest.Id);
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.NotPending", ex.Message),
                    Outcome.Invalid);
            }

            joinRequestRepository.Update(joinRequest);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict rejecting join request {JoinRequestId}.", joinRequest.Id);
                return Result.Failure<JoinRequestDto>(
                    new Error("JoinRequest.ConcurrencyConflict", "Join request was modified by another request. Please retry."),
                    Outcome.Conflict);
            }

            logger.LogInformation(
                "JoinRequest {JoinRequestId} rejected for booking {BookingId} by user {ViewerId} (admin={IsAdmin}).",
                joinRequest.Id, booking.Id, viewerId, isAdmin);

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
