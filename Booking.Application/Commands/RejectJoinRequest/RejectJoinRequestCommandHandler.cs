using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.RejectJoinRequest;

public sealed class RejectJoinRequestCommandHandler(
    IJoinRequestRepository joinRequestRepository,
    ITourBookingRepository tourBookingRepository,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RejectJoinRequestCommandHandler> logger)
    : ICommandHandler<RejectJoinRequestCommand>
{
    public async Task<Result> Handle(RejectJoinRequestCommand request, CancellationToken cancellationToken)
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

            var parentBooking = await tourBookingRepository
                .GetByIdAsync(joinRequest.TourBookingId, cancellationToken)
                .ConfigureAwait(false);

            if (parentBooking is null)
            {
                return Result.Failure(new Error("TourBooking.NotFound", "Parent booking not found."), Outcome.NotFound);
            }

            // Only the guide/booking owner can reject — Admin+ may override.
            // P1 (2026-05-30): added admin bypass for consistency with Confirm/Complete/Reject.
            var isAdmin = currentUser.HasPermission("Booking.AdminBookingDashboard.Update")
                || currentUser.HasPermission("Booking.AdminBookingDashboard.Read");

            if (!isAdmin
                && parentBooking.GuideId != responderId
                && parentBooking.UserId != responderId)
            {
                return Result.Failure(
                    new Error("JoinRequest.Unauthorized", "Only the guide or booking owner can reject join requests."),
                    Outcome.Forbidden);
            }

            var rejectResult = joinRequest.Reject(responderId, request.ResponseMessage, utcNow);
            if (!rejectResult.IsSuccess)
            {
                return rejectResult;
            }

            joinRequestRepository.Update(joinRequest);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cache.RemoveByTagAsync($"join-requests:booking:{joinRequest.TourBookingId:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync($"join-requests:user:{joinRequest.UserId:D}", cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "JoinRequest {JoinRequestId} rejected by {ResponderId}.",
                joinRequest.Id, responderId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Request was cancelled."), Outcome.Canceled);
        }
    }
}
