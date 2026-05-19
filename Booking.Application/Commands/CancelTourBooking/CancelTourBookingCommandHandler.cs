using System.Text.Json;
using Booking.Application.Interfaces;
using Booking.Contracts.Authorization;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Booking.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Application.Commands.CancelTourBooking;

/// <summary>
/// Cancels a booking. Source is determined from caller identity:
///  - Owner (booking.UserId == viewer) → User. Reason is optional. Refund walks policy snapshot.
///  - Admin (has AdminBookingDashboard.Update) → Admin. Reason mandatory ≥10 chars. 100% refund.
///  - Else if viewer is the provider that owns the tour → Provider. Reason mandatory. 100% refund.
///  - Else → 403 OwnerMismatch.
/// </summary>
public sealed class CancelTourBookingCommandHandler(
    ITourBookingRepository tourBookingRepository,
    IAvailabilitySlotRepository slotRepository,
    IBookingProviderSnapshotReader providerSnapshotReader,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CancelTourBookingCommandHandler> logger)
    : ICommandHandler<CancelTourBookingCommand, CancelTourBookingResult>
{
    public async Task<Result<CancelTourBookingResult>> Handle(CancelTourBookingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result.Failure<CancelTourBookingResult>(
                    new Error("TourBooking.Unauthorized", "Authentication is required to cancel a booking."),
                    Outcome.Unauthorized);
            }

            var viewerId = currentUser.UserId.Value;

            var booking = await tourBookingRepository
                .GetByIdWithDetailsAsync(request.BookingId, cancellationToken)
                .ConfigureAwait(false);
            if (booking is null)
            {
                return Result.Failure<CancelTourBookingResult>(
                    new Error("TourBooking.NotFound", "Booking was not found."),
                    Outcome.NotFound);
            }

            // Determine source
            var isAdmin = currentUser.HasPermission(
                $"{BookingFeatures.AdminBookingDashboard}.{AppAction.Update}");

            CancellationSource source;
            if (booking.UserId == viewerId)
            {
                source = CancellationSource.User;
            }
            else if (isAdmin)
            {
                source = CancellationSource.Admin;
            }
            else
            {
                var providerSnapshot = await providerSnapshotReader
                    .GetByIdAsync(booking.ProviderId, cancellationToken)
                    .ConfigureAwait(false);
                if (providerSnapshot is null || providerSnapshot.OwnerUserId != viewerId)
                {
                    logger.LogWarning(
                        "Cancel forbidden: viewer {ViewerId} is neither owner nor admin nor provider for booking {BookingId}.",
                        viewerId,
                        booking.Id);
                    return Result.Failure<CancelTourBookingResult>(
                        new Error("TourBooking.OwnerMismatch", "You are not authorized to cancel this booking."),
                        Outcome.Forbidden);
                }
                source = CancellationSource.Provider;
            }

            // Reason rules
            var reason = request.Reason?.Trim();
            if ((source == CancellationSource.Provider || source == CancellationSource.Admin)
                && (string.IsNullOrEmpty(reason) || reason.Length < 10))
            {
                return Result.Failure<CancelTourBookingResult>(
                    new Error("TourBooking.CancellationReasonRequired", "Cancellation reason (>=10 chars) is required for provider or admin cancellations."),
                    Outcome.Invalid);
            }

            // State guard (terminal states cannot be cancelled)
            if (booking.Status == BookingStatus.Cancelled
                || booking.Status == BookingStatus.Rejected
                || booking.Status == BookingStatus.Completed)
            {
                return Result.Failure<CancelTourBookingResult>(
                    new Error("TourBooking.InvalidState", $"Cannot cancel from state {booking.Status}."),
                    Outcome.Invalid);
            }

            // Load slot for refund-tier calc (also used by capacity-restore handler downstream)
            var slot = await slotRepository
                .GetByIdWithLockAsync(booking.AvailabilitySlotId, cancellationToken)
                .ConfigureAwait(false);
            if (slot is null)
            {
                return Result.Failure<CancelTourBookingResult>(
                    new Error("AvailabilitySlot.NotFound", "Slot for booking not found."),
                    Outcome.NotFound);
            }

            var slotStart = DateTime.SpecifyKind(slot.Date.ToDateTime(slot.StartTime), DateTimeKind.Utc);
            var timeUntilTour = slotStart - DateTime.UtcNow;

            // Compute refund pct
            var refundPct = source switch
            {
                CancellationSource.Provider => 100m,
                CancellationSource.Admin => 100m,
                CancellationSource.System => 0m,
                _ => ResolveRefundPercentage(booking.RefundPolicySnapshot, timeUntilTour),
            };

            var ctx = new BookingCancellationContext(
                Source: source,
                Reason: reason,
                ProviderInitiated: source == CancellationSource.Provider,
                ForceMajeureOverride: false);

            try
            {
                booking.Cancel(ctx, refundPct);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Domain rejected cancel for booking {BookingId}.", booking.Id);
                return Result.Failure<CancelTourBookingResult>(
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
                logger.LogWarning(ex, "Concurrency conflict cancelling booking {BookingId}.", booking.Id);
                return Result.Failure<CancelTourBookingResult>(
                    new Error("TourBooking.ConcurrencyConflict", "Booking was modified by another request. Please retry."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync($"booking:{booking.Id:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync($"bookings:user:{booking.UserId:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync("bookings:admin", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync($"availability:tour:{booking.TourId:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync($"availability:tour:{booking.TourId:D}:date:{slot.Date:yyyy-MM-dd}", cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Booking {BookingId} cancelled by source={Source} viewer={ViewerId} refund={Refund} {Currency}.",
                booking.Id, source, viewerId, booking.RefundAmount, booking.Currency);

            return Result.Success(new CancelTourBookingResult(
                BookingId: booking.Id,
                Status: booking.Status,
                CancelledAt: booking.CancelledAt!.Value,
                Source: booking.CancellationSource!.Value,
                Reason: booking.CancellationReason,
                RefundAmount: booking.RefundAmount!.Value,
                Currency: booking.Currency));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<CancelTourBookingResult>(
                new Error("Request.Cancelled", "Request was cancelled."),
                Outcome.Canceled);
        }
    }

    private static decimal ResolveRefundPercentage(string snapshotJson, TimeSpan timeUntilTour)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson) || snapshotJson == "{}")
            return 0m;

        try
        {
            using var doc = JsonDocument.Parse(snapshotJson);
            if (!doc.RootElement.TryGetProperty("tiers", out var tiers) || tiers.ValueKind != JsonValueKind.Array)
                return 0m;

            var hoursLeft = timeUntilTour.TotalHours;
            var winnerThreshold = -1;
            var winnerPct = 0m;

            foreach (var tier in tiers.EnumerateArray())
            {
                if (!tier.TryGetProperty("hoursBeforeTour", out var hoursEl)
                    || !tier.TryGetProperty("refundPercent", out var pctEl))
                {
                    continue;
                }

                if (!hoursEl.TryGetInt32(out var threshold)) continue;
                var pct = pctEl.ValueKind == JsonValueKind.Number ? pctEl.GetDecimal() : 0m;

                if (hoursLeft >= threshold && threshold > winnerThreshold)
                {
                    winnerThreshold = threshold;
                    winnerPct = pct;
                }
            }

            return winnerPct < 0m ? 0m : winnerPct;
        }
        catch (JsonException)
        {
            return 0m;
        }
    }
}
