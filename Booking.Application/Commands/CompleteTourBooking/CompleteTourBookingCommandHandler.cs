using Booking.Application.Interfaces;
using Booking.Contracts.Authorization;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Application.Commands.CompleteTourBooking;

public sealed class CompleteTourBookingCommandHandler(
    ITourBookingRepository tourBookingRepository,
    IAvailabilitySlotRepository slotRepository,
    IBookingProviderSnapshotReader providerSnapshotReader,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CompleteTourBookingCommandHandler> logger)
    : ICommandHandler<CompleteTourBookingCommand, CompleteTourBookingResult>
{
    public async Task<Result<CompleteTourBookingResult>> Handle(
        CompleteTourBookingCommand request,
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
                return Result.Failure<CompleteTourBookingResult>(
                    new Error("TourBooking.NotFound", "Booking was not found."),
                    Outcome.NotFound);
            }

            var isAdmin = currentUser.HasPermission(
                $"{BookingFeatures.AdminBookingDashboard}.{AppAction.Update}");

            if (!isAdmin)
            {
                var provider = await providerSnapshotReader
                    .GetByIdAsync(booking.ProviderId, cancellationToken)
                    .ConfigureAwait(false);
                if (provider is null || provider.OwnerUserId != viewerId)
                {
                    logger.LogWarning(
                        "Complete-booking denied: viewer {ViewerId} is neither owner of provider {ProviderId} nor admin.",
                        viewerId,
                        booking.ProviderId);
                    return Result.Failure<CompleteTourBookingResult>(
                        new Error(
                            "TourBooking.OwnerMismatch",
                            "Only the provider of the tour or an admin may complete this booking."),
                        Outcome.Forbidden);
                }
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                return Result.Failure<CompleteTourBookingResult>(
                    new Error(
                        "TourBooking.InvalidState",
                        $"Cannot complete booking in state {booking.Status}. Must be Confirmed."),
                    Outcome.Invalid);
            }

            var slot = await slotRepository
                .GetByIdWithLockAsync(booking.AvailabilitySlotId, cancellationToken)
                .ConfigureAwait(false);
            if (slot is null)
            {
                return Result.Failure<CompleteTourBookingResult>(
                    new Error("AvailabilitySlot.NotFound", "Availability slot was not found."),
                    Outcome.NotFound);
            }

            var slotStartUtc = DateTime.SpecifyKind(
                slot.Date.ToDateTime(slot.StartTime),
                DateTimeKind.Utc);
            if (slotStartUtc > DateTime.UtcNow)
            {
                return Result.Failure<CompleteTourBookingResult>(
                    new Error(
                        "TourBooking.NotYetStarted",
                        "Cannot complete a tour that has not yet started."),
                    Outcome.Invalid);
            }

            try
            {
                booking.Complete(viewerId);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Complete booking domain guard failed for {BookingId}.", booking.Id);
                return Result.Failure<CompleteTourBookingResult>(
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
                logger.LogWarning(ex, "Concurrency conflict completing booking {BookingId}.", booking.Id);
                return Result.Failure<CompleteTourBookingResult>(
                    new Error(
                        "TourBooking.ConcurrencyConflict",
                        "Booking was modified by another request. Reload and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                $"booking:{booking.Id:D}",
                cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                $"bookings:user:{booking.UserId:D}",
                cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                "bookings:admin",
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Booking {BookingId} completed by user {ViewerId} (admin={IsAdmin}) at {CompletedAt:O}.",
                booking.Id,
                viewerId,
                isAdmin,
                booking.CompletedAt);

            return Result.Success(new CompleteTourBookingResult(
                booking.Id,
                booking.Status,
                booking.CompletedAt!.Value,
                booking.CompletedByUserId!.Value));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<CompleteTourBookingResult>(
                new Error("Request.Cancelled", "Operation was cancelled."),
                Outcome.Canceled);
        }
    }
}
