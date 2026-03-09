using ContentCore.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;

namespace ContentCore.Infrastructure.EventHandlers;

/// <summary>
/// Handles AttachmentDeletedDomainEvent by deleting the physical file from storage.
/// </summary>
public sealed class AttachmentDeletedDomainEventHandler(
    IFileStorageService fileStorageService,
    ILogger<AttachmentDeletedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<AttachmentDeletedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<AttachmentDeletedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var deleted = await fileStorageService.DeleteAsync(evt.Url, ct);

        if (deleted)
        {
            logger.LogInformation(
                "AttachmentDeletedDomainEvent: Deleted file {Url} for attachment {AttachmentId} (Entity: {EntityType}/{EntityId}).",
                evt.Url, evt.AttachmentId, evt.EntityType, evt.EntityId);
        }
        else
        {
            logger.LogWarning(
                "AttachmentDeletedDomainEvent: File {Url} not found for attachment {AttachmentId}. May have been already deleted.",
                evt.Url, evt.AttachmentId);
        }
    }
}