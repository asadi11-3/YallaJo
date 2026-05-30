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

namespace Booking.Application.Commands.ConfirmTourBooking;

public sealed class ConfirmTourBookingCommandHandler(
    ITourBookingRepository tourBookingRepository,
    IBookingProviderSnapshotReader providerSnapshotReader,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ConfirmTourBookingCommandHandler> logger)
    : ICommandHandler<ConfirmTourBookingCommand, ConfirmTourBookingResult>
{
    public async Task<Result<ConfirmTourBookingResult>> Handle(
        ConfirmTourBookingCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var viewerId = currentUser.UserId!.Value;

            // Load the booking.
            var booking = await tourBookingRepository
                .GetByIdWithDetailsAsync(request.BookingId, cancellationToken)
                .ConfigureAwait(false);
            if (booking is null)
            {
                logger.LogWarning("ConfirmTourBooking rejected: booking {BookingId} not found.", request.BookingId);
                return Result.Failure<ConfirmTourBookingResult>(
                    new Error("TourBooking.NotFound", "Booking was not found."),
                    Outcome.NotFound);
            }

            // Ownership: provider of the tour OR admin.
            var isAdmin = currentUser.HasPermission("Booking.AdminBookingDashboard.Update")
                || currentUser.HasPermission("Booking.AdminBookingDashboard.Read");

            if (!isAdmin)
            {
                var providerSnapshot = await providerSnapshotReader
                    .GetByIdAsync(booking.ProviderId, cancellationToken)
                    .ConfigureAwait(false);
                if (providerSnapshot is null || providerSnapshot.OwnerUserId != viewerId)
                {
                    logger.LogWarning(
                        "ConfirmTourBooking rejected: viewer {ViewerId} is not the provider of booking {BookingId}.",
                        viewerId,
                        booking.Id);
                    return Result.Failure<ConfirmTourBookingResult>(
                        new Error("TourBooking.Forbidden", "Only the provider of this tour can confirm the booking."),
                        Outcome.Forbidden);
                }
            }

            // State guard (return Result.Failure BEFORE calling domain method; domain throws only as safety net).
            if (booking.Status is not (BookingStatus.AwaitingPayment or BookingStatus.PendingConfirmation))
            {
                logger.LogWarning(
                    "ConfirmTourBooking rejected: booking {BookingId} is in state {Status}.",
                    booking.Id,
                    booking.Status);
                return Result.Failure<ConfirmTourBookingResult>(
                    new Error("TourBooking.InvalidState", $"Cannot confirm booking in state {booking.Status}."),
                    Outcome.Invalid);
            }

            // Manual confirm = ConfirmationSource.Manual (auto-confirm uses .AutoAccept, payment webhook uses .PaymentWebhook).
            try
            {
                booking.Confirm(ConfirmationSource.Manual);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Domain guard rejected Confirm on {BookingId}.", booking.Id);
                return Result.Failure<ConfirmTourBookingResult>(
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
                logger.LogWarning("ConfirmTourBooking concurrency conflict on {BookingId}.", booking.Id);
                return Result.Failure<ConfirmTourBookingResult>(
                    new Error("TourBooking.ConcurrencyConflict", "The booking was modified concurrently. Reload and retry."),
                    Outcome.Conflict);
            }

            // Cache invalidation after SaveChanges per ERR-010 (most specific tags).
            await cache.RemoveByTagAsync($"booking:{booking.Id:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync($"bookings:user:{booking.UserId:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync("bookings:admin", cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Booking {BookingId} confirmed by {ViewerId}. Reference={Reference}, IsAdmin={IsAdmin}.",
                booking.Id,
                viewerId,
                booking.Reference,
                isAdmin);

            return Result.Success(new ConfirmTourBookingResult(
                booking.Id,
                booking.Status,
                booking.ConfirmedAt!.Value,
                booking.ConfirmationSource!.Value));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<ConfirmTourBookingResult>(
                new Error("Request.Cancelled", "The confirm request was cancelled by the caller."),
                Outcome.Canceled);
        }
    }
}
