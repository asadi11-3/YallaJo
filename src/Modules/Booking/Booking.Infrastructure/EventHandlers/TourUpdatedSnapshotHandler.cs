using Booking.Application.Interfaces;
using Booking.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Infrastructure.EventHandlers;

/// <summary>
/// Updates the Booking-owned TourSnapshot when a tour's details change.
/// Note: TourUpdatedIntegrationEvent only carries change flags, not new values.
/// We mark the snapshot as needing refresh; actual data comes from TourApproved.
/// </summary>
public sealed class TourUpdatedSnapshotHandler(
    BookingDbContext dbContext,
    IBookingUnitOfWork unitOfWork,
    ILogger<TourUpdatedSnapshotHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourUpdatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourUpdatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var existing = await dbContext.TourSnapshots
            .FirstOrDefaultAsync(s => s.TourId == evt.TourId, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            logger.LogDebug(
                "Booking: TourSnapshot not found for TourId={TourId} on TourUpdated — skipping.",
                evt.TourId);
            return;
        }

        // Refresh the snapshot with current values (no new data in this event)
        existing.Update(
            title: existing.Title,
            currency: existing.Currency,
            basePrice: existing.BasePrice,
            isActive: existing.IsActive,
            isApproved: existing.IsApproved,
            isInstantBooking: existing.IsInstantBooking,
            maxGroupSize: existing.MaxGroupSize);

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Booking: TourSnapshot LastUpdatedAt refreshed for TourId={TourId}.",
            evt.TourId);
    }
}
