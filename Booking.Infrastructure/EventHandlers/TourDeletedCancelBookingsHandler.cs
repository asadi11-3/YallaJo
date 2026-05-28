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
/// Cancels all active tour bookings for a tour when that tour is deleted.
/// </summary>
public sealed class TourDeletedCancelBookingsHandler(
    BookingDbContext dbContext,
    IBookingUnitOfWork unitOfWork,
    ILogger<TourDeletedCancelBookingsHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourDeletedIntegrationEvent>>
{
    private const int BatchSize = 100;

    public async Task Handle(
        IntegrationEventNotification<TourDeletedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;
        var cancellationCtx = new BookingCancellationContext(
            Source:               CancellationSource.Admin,
            Reason:               "Tour has been deleted",
            ProviderInitiated:    false,
            ForceMajeureOverride: true); // 100% refund for tour deletion

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
                .Where(b => b.TourId == evt.TourId && activeStatuses.Contains(b.Status))
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
                "Booking: cancelled batch of {Count} bookings for deleted TourId={TourId}.",
                batch.Count, evt.TourId);

            if (batch.Count < BatchSize)
            {
                break;
            }
        }

        logger.LogInformation(
            "Booking: Cancelled {Total} bookings for TourDeleted TourId={TourId}.",
            totalCancelled, evt.TourId);
    }
}
