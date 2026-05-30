using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Infrastructure.EventHandlers;

/// <summary>
/// Upserts the Booking-owned PricingTierSnapshot when a tour's pricing tier changes.
/// </summary>
public sealed class TourPricingTierChangedSnapshotHandler(
    BookingDbContext dbContext,
    IBookingUnitOfWork unitOfWork,
    ILogger<TourPricingTierChangedSnapshotHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourPricingTierChangedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourPricingTierChangedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        // Map TierId to TierType — the event carries TierId but we need TierType for the snapshot.
        // We use the TierId as a proxy for TierType since TierType is an enum (Standard=0, Premium=1, etc.)
        // The actual TierType mapping comes from the ContentTours domain; we store by TourId+TierType.
        // For now, we use a default TierType of Standard for new snapshots.
        // TODO: TourPricingTierChangedIntegrationEvent should carry TierType directly.

        if (evt.ChangeType == TourEntityChangeType.Deleted || evt.ChangeType == TourEntityChangeType.Deactivated)
        {
            // Remove the snapshot for this tier
            var toRemove = await dbContext.PricingTierSnapshots
                .Where(p => p.TourId == evt.TourId)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (toRemove.Count > 0)
            {
                dbContext.PricingTierSnapshots.RemoveRange(toRemove);
                await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                logger.LogInformation(
                    "Booking: PricingTierSnapshot(s) removed for TourId={TourId} (ChangeType={ChangeType}).",
                    evt.TourId, evt.ChangeType);
            }
            return;
        }

        // For Created/Updated: upsert using Standard tier as default
        var existing = await dbContext.PricingTierSnapshots
            .FirstOrDefaultAsync(p => p.TourId == evt.TourId && p.TierType == TierType.Adult, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var snapshot = PricingTierSnapshot.Create(
                tourId: evt.TourId,
                tierType: TierType.Adult,
                price: evt.Price,
                currency: evt.Currency);
            dbContext.PricingTierSnapshots.Add(snapshot);
        }
        else
        {
            existing.Update(evt.Price, evt.Currency);
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Booking: PricingTierSnapshot upserted for TourId={TourId} Price={Price} {Currency}.",
            evt.TourId, evt.Price, evt.Currency);
    }
}
