using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Contracts.IntegrationEvents;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class SupportTicketResolvedForUserHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<SupportTicketResolvedForUserHandler> logger)
    : INotificationHandler<IntegrationEventNotification<SupportTicketResolvedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<SupportTicketResolvedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;

        var evt = notification.Event;
        var ticket = await dbContext.SupportTickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == evt.TicketId, ct);
        if (ticket is null)
        {
            logger.LogWarning("Support ticket {TicketId} not found; skipping resolved notification", evt.TicketId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        dbContext.Notifications.Add(Notification.Create(
            userId: ticket.CreatedByUserId,
            type: NotificationType.SupportTicketResolved,
            channel: NotificationChannel.InApp,
            title: "Ticket Resolved",
            body: "Your support ticket has been resolved.",
            priority: NotificationPriority.Medium,
            entityType: "SupportTicket",
            entityId: evt.TicketId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
