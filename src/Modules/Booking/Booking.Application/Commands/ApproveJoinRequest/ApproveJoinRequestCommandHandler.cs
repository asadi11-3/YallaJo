using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.ApproveJoinRequest;

public sealed class ApproveJoinRequestCommandHandler(
    IJoinRequestRepository joinRequestRepository,
    ITourBookingRepository tourBookingRepository,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ApproveJoinRequestCommandHandler> logger)
    : ICommandHandler<ApproveJoinRequestCommand>
{
    public async Task<Result> Handle(ApproveJoinRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var responderId = currentUser.UserId!.Value;
            var utcNow = DateTime.UtcNow;

            var joinRequest = await joinRequestRepository
                .GetByIdAsync(request.JoinRequestId, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);

            if (joinRequest is null)
            {
                return Result.Failure(
                    new Error("JoinRequest.NotFound", "Join request not found."),
                    Outcome.NotFound);
            }

            // Verify responder owns the parent booking (is the guide/provider)
            var parentBooking = await tourBookingRepository
                .GetByIdAsync(joinRequest.TourBookingId, cancellationToken)
                .ConfigureAwait(false);

            if (parentBooking is null)
            {
                return Result.Failure(new Error("TourBooking.NotFound", "Parent booking not found."), Outcome.NotFound);
            }

            // Only the guide/booking owner can approve — Admin+ may override.
            // P1 (2026-05-30): added admin bypass for consistency with Confirm/Complete/Reject.
            var isAdmin = currentUser.HasPermission("Booking.AdminBookingDashboard.Update")
                || currentUser.HasPermission("Booking.AdminBookingDashboard.Read");

            if (!isAdmin
                && parentBooking.GuideId != responderId
                && parentBooking.UserId != responderId)
            {
                return Result.Failure(
                    new Error("JoinRequest.Unauthorized", "Only the guide or booking owner can approve join requests."),
                    Outcome.Forbidden);
            }

            var approveResult = joinRequest.Approve(responderId, request.ResponseMessage, utcNow);
            if (!approveResult.IsSuccess)
            {
                return approveResult;
            }

            joinRequestRepository.Update(joinRequest);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("JoinRequest.ConcurrencyConflict", "The join request was modified concurrently. Reload and retry."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync($"booking:{parentBooking.Id:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync($"join-requests:booking:{joinRequest.TourBookingId:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync($"join-requests:user:{joinRequest.UserId:D}", cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "JoinRequest {JoinRequestId} approved by {ResponderId}.",
                joinRequest.Id, responderId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Request was cancelled."), Outcome.Canceled);
        }
    }
}
