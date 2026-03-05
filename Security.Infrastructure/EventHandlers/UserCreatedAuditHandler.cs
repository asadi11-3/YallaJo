
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Contracts.IntegrationEvents;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Infrastructure.EventHandlers;


public sealed class UserCreatedAuditHandler(
    SecurityDbContext dbContext,
    ISecurityInboxStore inboxStore,
    ISecurityUnitOfWork unitOfWork,
    ILogger<UserCreatedAuditHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserCreatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<UserCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "Security: Message {MessageId} (UserCreated audit for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var ev = notification.Event;

        var auditLog = AuditLog.Create(
            userId: ev.UserId,
            action: "REGISTER",
            resourceType: "User",
            resourceId: ev.UserId);

        dbContext.AuditLogs.Add(auditLog);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Security: AuditLog written — REGISTER for user {UserId}.",
            ev.UserId);
    }
}
