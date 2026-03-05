
using Auth.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Infrastructure.EventHandlers;


public sealed class SessionRevokedAuditHandler(
    SecurityDbContext dbContext,
    ISecurityInboxStore inboxStore,
    ISecurityUnitOfWork unitOfWork,
    ILogger<SessionRevokedAuditHandler> logger)
    : INotificationHandler<IntegrationEventNotification<SessionRevokedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<SessionRevokedIntegrationEvent> notification,
        CancellationToken ct)
    {
        
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "Security: Message {MessageId} (SessionRevoked for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var ev = notification.Event;

        var auditLog = AuditLog.Create(
            userId: ev.UserId,
            action: "LOGOUT",
            resourceType: "Session",
            resourceId: ev.SessionId);

        dbContext.AuditLogs.Add(auditLog);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Security: AuditLog written — LOGOUT for user {UserId}, session {SessionId}.",
            ev.UserId, ev.SessionId);
    }
}
