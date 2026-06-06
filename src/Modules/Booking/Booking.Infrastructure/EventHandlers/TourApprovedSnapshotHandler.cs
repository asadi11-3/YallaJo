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
                title: evt.Title,
                currency: evt.Currency,
                basePrice: evt.BasePrice,
                isActive: true,
                isApproved: true,
                isInstantBooking: evt.IsInstantBooking,
                maxGroupSize: evt.MaxGroupSize);

            dbContext.TourSnapshots.Add(snapshot);
        }
        else
        {
            existing.Update(
                title: evt.Title,
                currency: evt.Currency,
                basePrice: evt.BasePrice,
                isActive: true,
                isApproved: true,
                isInstantBooking: evt.IsInstantBooking,
                maxGroupSize: evt.MaxGroupSize);
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Booking: TourSnapshot upserted for TourId={TourId} (Approved).",
            evt.TourId);
    }
}
