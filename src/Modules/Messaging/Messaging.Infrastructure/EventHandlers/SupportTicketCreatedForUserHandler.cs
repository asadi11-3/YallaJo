using MediatR;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Domain.Events;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class SupportTicketCreatedForUserHandler(
    MessagingDbContext dbContext,
    ILogger<SupportTicketCreatedForUserHandler> logger)
    : INotificationHandler<DomainEventNotification<TicketCreatedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TicketCreatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;
        dbContext.Notifications.Add(Notification.Create(
            userId: evt.CreatedByUserId,
            type: NotificationType.SupportTicketCreated,
            channel: NotificationChannel.InApp,
            title: "Support Ticket Created",
            body: "Your support ticket has been received. We'll respond shortly.",
            priority: NotificationPriority.Medium,
            entityType: "SupportTicket",
            entityId: evt.TicketId));

        logger.LogInformation("Queued support ticket created notification for user {UserId}, ticket {TicketId}", evt.CreatedByUserId, evt.TicketId);
        return Task.CompletedTask;
    }
}
