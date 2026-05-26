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

namespace Booking.Application.Commands.RejectTourBooking;

public sealed class RejectTourBookingCommandHandler(
    ITourBookingRepository tourBookingRepository,
    IBookingProviderSnapshotReader providerSnapshotReader,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RejectTourBookingCommandHandler> logger)
    : ICommandHandler<RejectTourBookingCommand, RejectTourBookingResult>
{
    public async Task<Result<RejectTourBookingResult>> Handle(
        RejectTourBookingCommand request,
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
                logger.LogWarning("RejectTourBooking rejected: booking {BookingId} not found.", request.BookingId);
                return Result.Failure<RejectTourBookingResult>(
                    new Error("TourBooking.NotFound", "Booking was not found."),
                    Outcome.NotFound);
            }

            var isAdmin = currentUser.HasPermission("Booking.AdminBookingDashboard.Update");
            if (!isAdmin)
            {
                var providerSnapshot = await providerSnapshotReader
                    .GetByIdAsync(booking.ProviderId, cancellationToken)
                    .ConfigureAwait(false);
                if (providerSnapshot is null || providerSnapshot.OwnerUserId != viewerId)
                {
                    logger.LogWarning(
                        "RejectTourBooking rejected: viewer {ViewerId} is not the provider of booking {BookingId}.",
                        viewerId,
                        booking.Id);
                    return Result.Failure<RejectTourBookingResult>(
                        new Error("TourBooking.Forbidden", "Only the provider of this tour can reject the booking."),
                        Outcome.Forbidden);
                }
            }

            if (booking.Status != BookingStatus.PendingConfirmation)
            {
                logger.LogWarning(
                    "RejectTourBooking rejected: booking {BookingId} is in state {Status}, must be PendingConfirmation.",
                    booking.Id,
                    booking.Status);
                return Result.Failure<RejectTourBookingResult>(
                    new Error("TourBooking.InvalidState", $"Cannot reject booking in state {booking.Status}; must be PendingConfirmation."),
                    Outcome.Invalid);
            }

            try
            {
                booking.Reject(request.Reason.Trim());
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Domain guard rejected Reject on {BookingId}.", booking.Id);
                return Result.Failure<RejectTourBookingResult>(
                    new Error("TourBooking.InvalidState", ex.Message),
                    Outcome.Invalid);
            }

            tourBookingRepository.Update(booking);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning("RejectTourBooking concurrency conflict on {BookingId}.", booking.Id);
                return Result.Failure<RejectTourBookingResult>(
                    new Error("TourBooking.ConcurrencyConflict", "The booking was modified concurrently. Reload and retry."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync($"booking:{booking.Id:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync($"bookings:user:{booking.UserId:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync("bookings:admin", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync($"availability:tour:{booking.TourId:D}", cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Booking {BookingId} rejected by {ViewerId}. Reference={Reference}, RefundAmount={RefundAmount} {Currency}, IsAdmin={IsAdmin}.",
                booking.Id,
                viewerId,
                booking.Reference,
                booking.RefundAmount,
                booking.Currency,
                isAdmin);

            return Result.Success(new RejectTourBookingResult(
                booking.Id,
                booking.Status,
                booking.RejectedAt!.Value,
                booking.RejectionReason!,
                booking.RefundAmount!.Value,
                booking.Currency));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<RejectTourBookingResult>(
                new Error("Request.Cancelled", "The reject request was cancelled by the caller."),
                Outcome.Canceled);
        }
    }
}
