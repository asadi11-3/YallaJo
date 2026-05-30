using Booking.Application.Interfaces;
using Booking.Domain.Enums;
using Booking.Domain.ValueObjects;
using Booking.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Infrastructure.EventHandlers;

/// <summary>
/// Cancels all active tour bookings for a specific guide-tour offering when that offering is suspended.
/// </summary>
public sealed class GuideOfferingSuspendedCancelBookingsHandler(
    BookingDbContext dbContext,
    IBookingUnitOfWork unitOfWork,
    ILogger<GuideOfferingSuspendedCancelBookingsHandler> logger)
    : INotificationHandler<IntegrationEventNotification<GuideTourOfferingSuspendedIntegrationEvent>>
{
    private const int BatchSize = 100;

    public async Task Handle(
        IntegrationEventNotification<GuideTourOfferingSuspendedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;
        var cancellationCtx = new BookingCancellationContext(
            Source:               CancellationSource.Admin,
            Reason:               $"Guide offering suspended: {evt.Reason}",
            ProviderInitiated:    false,
            ForceMajeureOverride: true); // 100% refund for offering-side suspensions

        var activeStatuses = new[]
        {
            BookingStatus.AwaitingPayment,
            BookingStatus.PendingConfirmation,
            BookingStatus.Confirmed
        };

        var totalCancelled = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            // Cancel bookings for this specific tour+guide combination
            var batch = await dbContext.TourBookings
                .Where(b => b.TourId == evt.TourId
                         && b.GuideId == evt.GuideUserId
                         && activeStatuses.Contains(b.Status))
                .OrderBy(b => b.Id)
                .Take(BatchSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var booking in batch)
            {
                try
                {
                    booking.Cancel(cancellationCtx, refundPercentage: 100m);
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogWarning(
                        "Booking: skipping cancel for Booking {BookingId} (already terminal): {Message}",
                        booking.Id, ex.Message);
                }
            }

            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            totalCancelled += batch.Count;

            logger.LogDebug(
                "Booking: cancelled batch of {Count} bookings for suspended offering TourId={TourId} GuideUserId={GuideUserId}.",
                batch.Count, evt.TourId, evt.GuideUserId);

            if (batch.Count < BatchSize)
            {
                break;
            }
        }

        logger.LogInformation(
            "Booking: Cancelled {Total} bookings for GuideOfferingSuspended TourId={TourId} GuideUserId={GuideUserId}.",
            totalCancelled, evt.TourId, evt.GuideUserId);
    }
}
