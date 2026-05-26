using Booking.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using Booking.Domain.Entities;
using Booking.Application.Interfaces;

namespace Booking.Infrastructure.EventHandlers;

/// <summary>
/// Populates the Booking-owned TourSnapshot table when a tour is approved.
/// This replaces the StubBookingTourSnapshotReader with real data.
/// </summary>
public sealed class TourApprovedSnapshotHandler(
    BookingDbContext dbContext,
    IBookingUnitOfWork unitOfWork,
    ILogger<TourApprovedSnapshotHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourApprovedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourApprovedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var existing = await dbContext.TourSnapshots
            .FirstOrDefaultAsync(s => s.TourId == evt.TourId, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var snapshot = TourSnapshot.Create(
                tourId: evt.TourId,
                providerId: evt.CreatedByUserId,
                title: string.Empty, // Will be updated by TourUpdated event
                currency: "JOD",
                basePrice: 0m,
                isActive: true,
                isApproved: true,
                isInstantBooking: false);

            dbContext.TourSnapshots.Add(snapshot);
        }
        else
        {
            existing.Update(
                title: existing.Title,
                currency: existing.Currency,
                basePrice: existing.BasePrice,
                isActive: true,
                isApproved: true,
                isInstantBooking: existing.IsInstantBooking);
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Booking: TourSnapshot upserted for TourId={TourId} (Approved).",
            evt.TourId);
    }
}
