using Booking.Application.Interfaces;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Application.Commands.OpenBookingDispute;

/// <summary>
/// G4a: only the booking owner may open a dispute. Domain guards the 48h window + Completed precondition.
/// </summary>
public sealed class OpenBookingDisputeCommandHandler(
    ITourBookingRepository tourBookingRepository,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<OpenBookingDisputeCommandHandler> logger)
    : ICommandHandler<OpenBookingDisputeCommand, OpenBookingDisputeResult>
{
    public async Task<Result<OpenBookingDisputeResult>> Handle(
        OpenBookingDisputeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var viewerId = currentUser.UserId!.Value;

            var booking = await tourBookingRepository
                .GetByIdWithDetailsAsync(request.BookingId, cancellationToken)
                .ConfigureAwait(false);
            if (booking is null)
            {
                return Result.Failure<OpenBookingDisputeResult>(
                    new Error("TourBooking.NotFound", "Booking was not found."),
                    Outcome.NotFound);
            }

            if (booking.UserId != viewerId)
            {
                logger.LogWarning(
                    "Open-dispute denied: viewer {ViewerId} is not the owner of booking {BookingId}.",
                    viewerId,
                    booking.Id);
                return Result.Failure<OpenBookingDisputeResult>(
                    new Error(
                        "TourBooking.OwnerMismatch",
                        "Only the booking owner may open a dispute."),
                    Outcome.Forbidden);
            }

            if (booking.Status != BookingStatus.Completed)
            {
                return Result.Failure<OpenBookingDisputeResult>(
                    new Error(
                        "TourBooking.InvalidState",
                        $"Cannot open dispute from state {booking.Status}. Must be Completed."),
                    Outcome.Invalid);
            }

            try
            {
                booking.OpenDispute(viewerId, request.Reason);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Open-dispute domain guard failed for {BookingId}.", booking.Id);
                return Result.Failure<OpenBookingDisputeResult>(
                    new Error("TourBooking.DisputeRejected", ex.Message),
                    Outcome.Invalid);
            }

            tourBookingRepository.Update(booking);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict opening dispute on booking {BookingId}.", booking.Id);
                return Result.Failure<OpenBookingDisputeResult>(
                    new Error(
                        "TourBooking.ConcurrencyConflict",
                        "Booking was modified by another request. Reload and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync($"booking:{booking.Id:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync($"bookings:user:{booking.UserId:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync("bookings:admin", cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Booking {BookingId} disputed by user {ViewerId} at {DisputedAt:O}.",
                booking.Id,
                viewerId,
                booking.DisputedAt);

            return Result.Success(new OpenBookingDisputeResult(
                booking.Id,
                booking.Status,
                booking.DisputedAt!.Value,
                booking.DisputeReason!));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<OpenBookingDisputeResult>(
                new Error("Request.Cancelled", "Operation was cancelled."),
                Outcome.Canceled);
        }
    }
}
