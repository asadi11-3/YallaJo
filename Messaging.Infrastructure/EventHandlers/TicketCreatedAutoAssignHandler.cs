using MediatR;
using Messaging.Domain.Events;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class TicketCreatedAutoAssignHandler(
    MessagingDbContext dbContext,
    ILogger<TicketCreatedAutoAssignHandler> logger)
    : INotificationHandler<DomainEventNotification<TicketCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<TicketCreatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var admin = await dbContext.AdminAssignmentRosters
            .Where(r => r.IsActive && !r.IsOnLeave)
            .OrderBy(r => r.LastAssignedAt)
            .FirstOrDefaultAsync(ct);

        if (admin is null)
        {
            logger.LogWarning("No active admin available to auto-assign support ticket {TicketId}", evt.TicketId);
            return;
        }

        var ticket = await dbContext.SupportTickets.FindAsync([evt.TicketId], ct);
        if (ticket is null)
        {
            logger.LogWarning("Support ticket {TicketId} was not found for auto-assignment", evt.TicketId);
            return;
        }

        var now = DateTime.UtcNow;
        ticket.AssignTo(admin.AdminUserId, Guid.Empty, now);
        admin.RecordAssignment(now);

        logger.LogInformation(
            "Auto-assigned support ticket {TicketId} to admin {AdminUserId}",
            evt.TicketId,
            admin.AdminUserId);
    }
}
