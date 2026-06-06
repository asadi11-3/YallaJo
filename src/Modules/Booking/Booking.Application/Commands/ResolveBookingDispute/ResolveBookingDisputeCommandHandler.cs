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

namespace Booking.Application.Commands.ResolveBookingDispute;

/// <summary>
/// G4a: admin-only via route permission. Domain guards Disputed precondition + reason length.
/// </summary>
public sealed class ResolveBookingDisputeCommandHandler(
    ITourBookingRepository tourBookingRepository,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ResolveBookingDisputeCommandHandler> logger)
    : ICommandHandler<ResolveBookingDisputeCommand, ResolveBookingDisputeResult>
{
    public async Task<Result<ResolveBookingDisputeResult>> Handle(
        ResolveBookingDisputeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var adminId = currentUser.UserId!.Value;

            var booking = await tourBookingRepository
                .GetByIdWithDetailsAsync(request.BookingId, cancellationToken)
                .ConfigureAwait(false);
            if (booking is null)
            {
                return Result.Failure<ResolveBookingDisputeResult>(
                    new Error("TourBooking.NotFound", "Booking was not found."),
                    Outcome.NotFound);
            }

            if (booking.Status != BookingStatus.Disputed)
            {
                return Result.Failure<ResolveBookingDisputeResult>(
                    new Error(
                        "TourBooking.InvalidState",
                        $"Cannot resolve dispute from state {booking.Status}. Must be Disputed."),
                    Outcome.Invalid);
            }

            try
            {
                booking.ResolveDispute(adminId, request.ResolutionNotes);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Resolve-dispute domain guard failed for {BookingId}.", booking.Id);
                return Result.Failure<ResolveBookingDisputeResult>(
                    new Error("TourBooking.DisputeResolveRejected", ex.Message),
                    Outcome.Invalid);
            }

            tourBookingRepository.Update(booking);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict resolving dispute on booking {BookingId}.", booking.Id);
                return Result.Failure<ResolveBookingDisputeResult>(
                    new Error(
                        "TourBooking.ConcurrencyConflict",
                        "Booking was modified by another request. Reload and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync($"booking:{booking.Id:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync($"bookings:user:{booking.UserId:D}", cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync("bookings:admin", cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Booking {BookingId} dispute resolved by admin {AdminId} at {ResolvedAt:O}.",
                booking.Id,
                adminId,
                booking.ResolvedAt);

            return Result.Success(new ResolveBookingDisputeResult(
                booking.Id,
                booking.Status,
                booking.ResolvedAt!.Value,
                booking.ResolvedByAdminId!.Value,
                booking.ResolutionNotes!));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<ResolveBookingDisputeResult>(
                new Error("Request.Cancelled", "Operation was cancelled."),
                Outcome.Canceled);
        }
    }
}
