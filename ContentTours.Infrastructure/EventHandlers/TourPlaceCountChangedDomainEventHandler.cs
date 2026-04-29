using ContentTours.Contracts.IntegrationEvents;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

/// <summary>
/// Handles <see cref="TourPlaceCountChangedDomainEvent"/>.
///
/// Re-queries the live count of active (Published, not deleted) tours for the affected
/// Place from <see cref="ContentToursDbContext"/>, then writes a
/// <see cref="PlaceTourCountUpdatedIntegrationEvent"/> to the outbox so ContentPlaces
/// can update its denormalized <c>Place.TourCount</c> column.
///
/// Re-querying the count (rather than incrementing/decrementing) makes the event
/// idempotent and tolerant of retries or out-of-order processing.
///
/// Does NOT call SaveChangesAsync — UoW commits atomically.
/// </summary>
public sealed class TourPlaceCountChangedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourPlaceCountChangedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourPlaceCountChangedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<TourPlaceCountChangedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        if (evt.PlaceId is null)
        {
            // Tour was never linked to a place — nothing to update.
            return;
        }

        var placeId = evt.PlaceId.Value;

        // Count live: Published tours linked to this place that are not soft-deleted.
        // Excludes the current tour if it was just soft-deleted (IsDeleted = true is
        // excluded by the EF query filter on AuditableEntity).
        var activeCount = await dbContext.Tours
            .CountAsync(t => t.PlaceId == placeId && t.Status == TourStatus.Published, ct);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new PlaceTourCountUpdatedIntegrationEvent(placeId, activeCount)));

        logger.LogInformation(
            "TourPlaceCountChanged: PlaceId={PlaceId} new active count={Count} (triggered by Tour {TourId})",
            placeId, activeCount, evt.TourId);
    }
}
