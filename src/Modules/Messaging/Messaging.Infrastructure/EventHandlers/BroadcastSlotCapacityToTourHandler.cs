using Booking.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>
/// Pushes live tour availability to the public tour-detail page over SignalR (UI-PERF S2 / CAL3 / RT1).
///
/// Subscribes to the <b>integration</b> event (outbox-delivered, POST-COMMIT) rather than the domain
/// event, because domain events are dispatched BEFORE <c>SaveChanges</c> in this codebase
/// (<c>UnitOfWork</c>) — broadcasting from the pre-commit path could push a capacity that later rolls
/// back. The outbox guarantees the change is durably committed before this handler runs.
///
/// Emits <c>SlotCapacityChanged { slotId, remainingCapacity }</c> to the <c>tour:{tourId}</c> group
/// only (S5). Send IDs only (S3). Group membership is owned by <see cref="TourSlotsHub"/>.
/// </summary>
public sealed class BroadcastSlotCapacityToTourHandler(
    IHubContext<TourSlotsHub> hub,
    ILogger<BroadcastSlotCapacityToTourHandler> logger)
    : INotificationHandler<IntegrationEventNotification<AvailabilitySlotCapacityChangedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<AvailabilitySlotCapacityChangedIntegrationEvent> notification,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        // Only tour-bound slots have a live audience; ignore tour-less slots.
        if (e.TourId is not { } tourId || tourId == Guid.Empty)
        {
            return;
        }

        // Skip no-op capacity transitions (e.g. confirm that nets to the same AvailableCount).
        if (e.OldCapacity == e.NewCapacity)
        {
            return;
        }

        await hub.Clients
            .Group(TourSlotsHub.TourGroup(tourId))
            .SendAsync(
                "SlotCapacityChanged",
                new { slotId = e.SlotId, remainingCapacity = e.NewCapacity },
                ct)
            .ConfigureAwait(false);

        logger.LogDebug(
            "Broadcast SlotCapacityChanged to tour:{TourId} for slot {SlotId} (remaining {Remaining}).",
            tourId, e.SlotId, e.NewCapacity);
    }
}
