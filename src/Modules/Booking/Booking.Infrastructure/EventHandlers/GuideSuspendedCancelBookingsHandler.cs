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
/// Cancels all active tour bookings assigned to a guide when that guide is suspended.
/// Mirrors <see cref="ProviderSuspendedCancelBookingsHandler"/> but filters by GuideId.
/// </summary>
public sealed class GuideSuspendedCancelBookingsHandler(
    BookingDbContext dbContext,
    IBookingUnitOfWork unitOfWork,
    ILogger<GuideSuspendedCancelBookingsHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourGuideSuspendedIntegrationEvent>>
{
    private const int BatchSize = 100;

    public async Task Handle(
        IntegrationEventNotification<TourGuideSuspendedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;
        var cancellationCtx = new BookingCancellationContext(
            Source:               CancellationSource.Admin,
            Reason:               $"Tour guide account suspended: {evt.Reason}",
            ProviderInitiated:    false,
            ForceMajeureOverride: true); // 100% refund for guide-side suspensions

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

            var batch = await dbContext.TourBookings
                .Where(b => b.GuideId == evt.UserId && activeStatuses.Contains(b.Status))
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
                "Booking: cancelled batch of {Count} bookings for guide UserId={UserId}.",
                batch.Count, evt.UserId);

            if (batch.Count < BatchSize)
            {
                break;
            }
        }

        logger.LogInformation(
            "Booking: Cancelled {Total} bookings for GuideSuspended UserId={UserId} TourGuideId={TourGuideId}.",
            totalCancelled, evt.UserId, evt.TourGuideId);
    }
}
