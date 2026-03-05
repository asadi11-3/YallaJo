// Security.Infrastructure/EventHandlers/PasswordChangedAuditHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Contracts.IntegrationEvents;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Infrastructure.EventHandlers;

/// <summary>
/// Writes an AuditLog entry whenever a user changes their password.
/// Reacts to PasswordChangedIntegrationEvent published by Security's own outbox.
/// </summary>
public sealed class PasswordChangedAuditHandler(
    SecurityDbContext dbContext,
    ISecurityInboxStore inboxStore,
    ISecurityUnitOfWork unitOfWork,
    ILogger<PasswordChangedAuditHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PasswordChangedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<PasswordChangedIntegrationEvent> notification,
        CancellationToken ct)
    {
        // Inbox idempotency guard
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "Security: Message {MessageId} (PasswordChanged for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var ev = notification.Event;

        var auditLog = AuditLog.Create(
            userId: ev.UserId,
            action: "PASSWORD_CHANGED",
            resourceType: "User",
            resourceId: ev.UserId);

        dbContext.AuditLogs.Add(auditLog);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Security: AuditLog written — PASSWORD_CHANGED for user {UserId}.",
            ev.UserId);
    }
}
