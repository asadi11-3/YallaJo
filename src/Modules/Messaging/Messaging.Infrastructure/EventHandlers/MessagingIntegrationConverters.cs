using MediatR;
using Messaging.Contracts.IntegrationEvents;
using Messaging.Domain.Events;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>
/// Converts <see cref="TicketCreatedDomainEvent"/> into <c>messaging.ticket.created.v1</c>.
/// Dispatched BEFORE SaveChanges inside the same UoW so the OutboxMessage row is persisted atomically.
/// </summary>
internal sealed class PublishTicketCreatedHandler(
    IMessagingOutboxWriter outbox,
    ILogger<PublishTicketCreatedHandler> logger)
    : INotificationHandler<DomainEventNotification<TicketCreatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TicketCreatedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new TicketCreatedIntegrationEvent(
            TicketId: e.TicketId,
            CreatedByUserId: e.CreatedByUserId,
            Category: e.Category.ToString(),
            Priority: e.Priority.ToString(),
            Subject: e.Subject,
            SlaBreachAt: e.SlaBreachAt,
            CreatedAt: e.OccurredOn);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued messaging.ticket.created.v1 for Ticket {TicketId}", e.TicketId);
    }
}

/// <summary>Converts <see cref="SupportTicketAssignedDomainEvent"/> into <c>messaging.ticket.assigned.v1</c>.</summary>
internal sealed class PublishTicketAssignedHandler(
    IMessagingOutboxWriter outbox,
    ILogger<PublishTicketAssignedHandler> logger)
    : INotificationHandler<DomainEventNotification<SupportTicketAssignedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<SupportTicketAssignedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new TicketAssignedIntegrationEvent(
            TicketId: e.TicketId,
            AssignedToUserId: e.AssignedToUserId,
            AssignedByUserId: e.AssignedByUserId,
            AssignedAt: e.AssignedAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued messaging.ticket.assigned.v1 for Ticket {TicketId}", e.TicketId);
    }
}

/// <summary>Converts <see cref="SupportTicketResolvedDomainEvent"/> into <c>messaging.ticket.resolved.v1</c>.</summary>
internal sealed class PublishTicketResolvedHandler(
    IMessagingOutboxWriter outbox,
    ILogger<PublishTicketResolvedHandler> logger)
    : INotificationHandler<DomainEventNotification<SupportTicketResolvedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<SupportTicketResolvedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new SupportTicketResolvedIntegrationEvent(
            TicketId: e.TicketId,
            ResolvedByUserId: e.ResolvedByUserId);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued messaging.ticket.resolved.v1 for Ticket {TicketId}", e.TicketId);
    }
}

/// <summary>Converts <see cref="NotificationDeliveredDomainEvent"/> into <c>messaging.notification.delivered.v1</c>.</summary>
internal sealed class PublishNotificationDeliveredHandler(
    IMessagingOutboxWriter outbox,
    ILogger<PublishNotificationDeliveredHandler> logger)
    : INotificationHandler<DomainEventNotification<NotificationDeliveredDomainEvent>>
{
    public async Task Handle(DomainEventNotification<NotificationDeliveredDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new NotificationDeliveredIntegrationEvent(
            NotificationId: e.NotificationId,
            UserId: e.UserId,
            Channel: e.Channel.ToString(),
            SentAt: e.DeliveredAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued messaging.notification.delivered.v1 for Notification {NotificationId}", e.NotificationId);
    }
}

/// <summary>Converts <see cref="NotificationFailedDomainEvent"/> into <c>messaging.notification.failed.v1</c>.</summary>
internal sealed class PublishNotificationFailedHandler(
    IMessagingOutboxWriter outbox,
    ILogger<PublishNotificationFailedHandler> logger)
    : INotificationHandler<DomainEventNotification<NotificationFailedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<NotificationFailedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new NotificationFailedIntegrationEvent(
            NotificationId: e.NotificationId,
            UserId: e.UserId,
            Channel: e.Channel.ToString(),
            FailureReason: e.FailureReason);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued messaging.notification.failed.v1 for Notification {NotificationId}", e.NotificationId);
    }
}
