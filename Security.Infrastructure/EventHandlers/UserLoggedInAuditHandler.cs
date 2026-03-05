// Security.Infrastructure/EventHandlers/UserLoggedInAuditHandler.cs

using Auth.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Infrastructure.EventHandlers;

/// <summary>
/// Writes an AuditLog entry whenever a user successfully logs in.
/// Reacts to UserLoggedInIntegrationEvent published by the Auth module.
/// </summary>
public sealed class UserLoggedInAuditHandler(
    SecurityDbContext dbContext,
    ISecurityInboxStore inboxStore,
    ISecurityUnitOfWork unitOfWork,
    ILogger<UserLoggedInAuditHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserLoggedInIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<UserLoggedInIntegrationEvent> notification,
        CancellationToken ct)
    {
        // Inbox idempotency guard
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "Security: Message {MessageId} (UserLoggedIn for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var ev = notification.Event;

        var auditLog = AuditLog.Create(
            userId: ev.UserId,
            action: "LOGIN",
            resourceType: "Session",
            resourceId: ev.SessionId,
            ipAddress: ev.IpAddress);

        dbContext.AuditLogs.Add(auditLog);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Security: AuditLog written — LOGIN for user {UserId}, session {SessionId}.",
            ev.UserId, ev.SessionId);
    }
}
