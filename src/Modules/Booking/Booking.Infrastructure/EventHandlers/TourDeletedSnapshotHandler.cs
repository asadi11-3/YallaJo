using Booking.Application.Interfaces;
using Booking.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Infrastructure.EventHandlers;

/// <summary>
/// Marks the TourSnapshot as inactive when a tour is deleted.
/// We keep the snapshot row for historical booking references.
/// </summary>
public sealed class TourDeletedSnapshotHandler(
    BookingDbContext dbContext,
    IBookingUnitOfWork unitOfWork,
    ILogger<TourDeletedSnapshotHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourDeletedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourDeletedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var existing = await dbContext.TourSnapshots
            .FirstOrDefaultAsync(s => s.TourId == evt.TourId, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            logger.LogDebug(
                "Booking: TourSnapshot not found for TourId={TourId} on TourDeleted — skipping.",
                evt.TourId);
            return;
        }

        existing.MarkInactive();
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Booking: TourSnapshot marked inactive for TourId={TourId} (Deleted).",
            evt.TourId);
    }
}
