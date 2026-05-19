using Booking.Application.Interfaces;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Booking.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Application.Commands.AdminForceRefund;

public sealed class AdminForceRefundCommandHandler(
    ITourBookingRepository tourBookingRepository,
    IAvailabilitySlotRepository slotRepository,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<AdminForceRefundCommandHandler> logger)
    : ICommandHandler<AdminForceRefundCommand, AdminForceRefundResult>
{
    public async Task<Result<AdminForceRefundResult>> Handle(
        AdminForceRefundCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Admin permission is gated by MustHavePermissionAttribute(AdminBookingDashboard, Update)
            // at the endpoint. Still need an authenticated user for audit trail.
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result.Failure<AdminForceRefundResult>(
                    new Error("TourBooking.Unauthorized", "Authentication is required for force refund."),
                    Outcome.Unauthorized);
            }

            var adminUserId = currentUser.UserId.Value;

            var booking = await tourBookingRepository
                .GetByIdWithDetailsAsync(request.BookingId, cancellationToken)
                .ConfigureAwait(false);
            if (booking is null)
            {
                return Result.Failure<AdminForceRefundResult>(
                    new Error("TourBooking.NotFound", "Booking was not found."),
                    Outcome.NotFound);
            }

            // Already-terminal bookings cannot be force-refunded.
            if (booking.Status is BookingStatus.Cancelled or BookingStatus.Rejected or BookingStatus.Completed)
            {
                return Result.Failure<AdminForceRefundResult>(
                    new Error(
                        "TourBooking.InvalidState",
                        $"Cannot force refund booking in state {booking.Status}."),
                    Outcome.Invalid);
            }

            // Ensure slot exists so the capacity-restore event handler can do its job.
            var slot = await slotRepository
                .GetByIdWithLockAsync(booking.AvailabilitySlotId, cancellationToken)
                .ConfigureAwait(false);
            if (slot is null)
            {
                return Result.Failure<AdminForceRefundResult>(
                    new Error("AvailabilitySlot.NotFound", "Availability slot was not found."),
                    Outcome.NotFound);
            }

            var reason = request.Reason.Trim();

            var ctx = new BookingCancellationContext(
                Source: CancellationSource.Admin,
                Reason: reason,
                ProviderInitiated: false,
                ForceMajeureOverride: true);

            try
            {
                // refundPercentage is forced to 100 by the domain method when ForceMajeureOverride=true.
                booking.Cancel(ctx, 100m);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Force refund domain guard failed for {BookingId}.", booking.Id);
                return Result.Failure<AdminForceRefundResult>(
                    new Error("TourBooking.InvalidState", ex.Message),
                    Outcome.Invalid);
            }

            tourBookingRepository.Update(booking);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict force-refunding booking {BookingId}.", booking.Id);
                return Result.Failure<AdminForceRefundResult>(
                    new Error(
                        "TourBooking.ConcurrencyConflict",
                        "Booking was modified by another request. Reload and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                $"booking:{booking.Id:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                $"bookings:user:{booking.UserId:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                "bookings:admin", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                $"availability:tour:{booking.TourId:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                $"availability:tour:{booking.TourId:D}:date:{slot.Date:yyyy-MM-dd}",
                cancellationToken).ConfigureAwait(false);

            logger.LogWarning(
                "FORCE REFUND: booking {BookingId} cancelled by admin {AdminUserId} with reason: {Reason}. Refund={Refund:F2} {Currency}.",
                booking.Id,
                adminUserId,
                reason,
                booking.RefundAmount,
                booking.Currency);

            return Result.Success(new AdminForceRefundResult(
                booking.Id,
                booking.Status,
                booking.CancelledAt!.Value,
                booking.CancellationSource!.Value,
                booking.CancellationReason ?? reason,
                booking.RefundAmount ?? booking.TotalAmount,
                booking.Currency));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<AdminForceRefundResult>(
                new Error("Request.Cancelled", "Operation was cancelled."),
                Outcome.Canceled);
        }
    }
}
