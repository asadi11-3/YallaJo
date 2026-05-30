// Security.Infrastructure/EventHandlers/PasswordResetAuditHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Contracts.IntegrationEvents;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Infrastructure.EventHandlers;

public sealed class PasswordResetAuditHandler(
    SecurityDbContext dbContext,
    ISecurityInboxStore inboxStore,
    ISecurityUnitOfWork unitOfWork,
    ILogger<PasswordResetAuditHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PasswordResetIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<PasswordResetIntegrationEvent> notification,
        CancellationToken cancellationToken)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, cancellationToken))
        {
            logger.LogWarning(
                "Security: Message {MessageId} (PasswordReset for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var ev = notification.Event;

        var auditLog = AuditLog.Create(
            userId: ev.UserId,
            action: "PASSWORD_RESET",
            resourceType: "User",
            resourceId: ev.UserId);

        dbContext.AuditLogs.Add(auditLog);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Security: AuditLog written — PASSWORD_RESET for user {UserId}.",
            ev.UserId);
    }
}
