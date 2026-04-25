using ContentCore.Contracts.IntegrationEvents;
using ContentCore.Domain.Events;
using ContentCore.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentCore.Infrastructure.EventHandlers;

/// <summary>
/// Handles AttachmentDeletedDomainEvent:
///   1. Publishes <see cref="AttachmentDeletedIntegrationEvent"/> to the outbox so downstream
///      modules (e.g. ContentSeo) can clear cached OgImageUrl when the primary image is removed.
///   2. Logs the deletion for auditing.
///
/// Physical file deletion is handled post-commit in DeleteAttachmentCommandHandler (not here).
/// </summary>
public sealed class AttachmentDeletedDomainEventHandler(
    ContentCoreDbContext dbContext,
    ILogger<AttachmentDeletedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<AttachmentDeletedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<AttachmentDeletedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        logger.LogInformation(
            "AttachmentDeletedDomainEvent: queueing outbox for attachment {AttachmentId} ({EntityType}/{EntityId}).",
            evt.AttachmentId, evt.EntityType, evt.EntityId);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new AttachmentDeletedIntegrationEvent(
                evt.AttachmentId,
                evt.EntityType.ToString(),
                evt.EntityId,
                evt.AttachmentType.ToString(),
                evt.Url)));

        return Task.CompletedTask;
    }
}