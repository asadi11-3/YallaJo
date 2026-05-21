using Booking.Contracts.IntegrationEvents;
using Booking.Domain.Events;
using Booking.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Infrastructure.EventHandlers;

/// <summary>
/// Converts <see cref="JoinRequestCreatedDomainEvent"/> into the outbound integration event.
/// </summary>
internal sealed class PublishJoinRequestCreatedHandler(
    IBookingOutboxWriter outbox,
    ILogger<PublishJoinRequestCreatedHandler> logger)
    : INotificationHandler<DomainEventNotification<JoinRequestCreatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<JoinRequestCreatedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new JoinRequestCreatedIntegrationEvent(
            JoinRequestId: e.JoinRequestId,
            TourBookingId: e.TourBookingId,
            UserId: e.UserId);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued booking.join-request.created.v1 for JoinRequest {Id}", e.JoinRequestId);
    }
}

/// <summary>Converts <see cref="JoinRequestApprovedDomainEvent"/> into the outbound integration event.</summary>
internal sealed class PublishJoinRequestApprovedHandler(
    IBookingOutboxWriter outbox,
    ILogger<PublishJoinRequestApprovedHandler> logger)
    : INotificationHandler<DomainEventNotification<JoinRequestApprovedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<JoinRequestApprovedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new JoinRequestApprovedIntegrationEvent(
            JoinRequestId: e.JoinRequestId,
            TourBookingId: e.TourBookingId,
            UserId: e.UserId);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued booking.join-request.approved.v1 for JoinRequest {Id}", e.JoinRequestId);
    }
}

/// <summary>Converts <see cref="JoinRequestRejectedDomainEvent"/> into the outbound integration event.</summary>
internal sealed class PublishJoinRequestRejectedHandler(
    IBookingOutboxWriter outbox,
    ILogger<PublishJoinRequestRejectedHandler> logger)
    : INotificationHandler<DomainEventNotification<JoinRequestRejectedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<JoinRequestRejectedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new JoinRequestRejectedIntegrationEvent(
            JoinRequestId: e.JoinRequestId,
            TourBookingId: e.TourBookingId,
            UserId: e.UserId,
            Reason: e.Reason);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued booking.join-request.rejected.v1 for JoinRequest {Id}", e.JoinRequestId);
    }
}

/// <summary>Converts <see cref="SlotLockCreatedDomainEvent"/> into the outbound integration event.</summary>
internal sealed class PublishSlotLockCreatedHandler(
    IBookingOutboxWriter outbox,
    ILogger<PublishSlotLockCreatedHandler> logger)
    : INotificationHandler<DomainEventNotification<SlotLockCreatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<SlotLockCreatedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new SlotLockCreatedIntegrationEvent(
            SlotLockId: e.SlotLockId,
            AvailabilitySlotId: e.AvailabilitySlotId,
            UserId: e.UserId,
            ExpiresAt: e.ExpiresAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued booking.slot-lock.created.v1 for SlotLock {Id}", e.SlotLockId);
    }
}

/// <summary>Converts <see cref="SlotLockReleasedDomainEvent"/> into the outbound integration event.</summary>
internal sealed class PublishSlotLockReleasedHandler(
    IBookingOutboxWriter outbox,
    ILogger<PublishSlotLockReleasedHandler> logger)
    : INotificationHandler<DomainEventNotification<SlotLockReleasedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<SlotLockReleasedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new SlotLockReleasedIntegrationEvent(
            SlotLockId: e.SlotLockId,
            AvailabilitySlotId: e.AvailabilitySlotId);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued booking.slot-lock.released.v1 for SlotLock {Id}", e.SlotLockId);
    }
}

/// <summary>Converts <see cref="AvailabilitySlotCapacityChangedDomainEvent"/> into the outbound integration event.</summary>
internal sealed class PublishAvailabilitySlotCapacityChangedHandler(
    IBookingOutboxWriter outbox,
    ILogger<PublishAvailabilitySlotCapacityChangedHandler> logger)
    : INotificationHandler<DomainEventNotification<AvailabilitySlotCapacityChangedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<AvailabilitySlotCapacityChangedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new AvailabilitySlotCapacityChangedIntegrationEvent(
            SlotId: e.SlotId,
            OldCapacity: e.OldCapacity,
            NewCapacity: e.NewCapacity);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation(
            "Enqueued booking.availability-slot.capacity-changed.v1 for Slot {SlotId} ({Old}=>{New})",
            e.SlotId, e.OldCapacity, e.NewCapacity);
    }
}
