using ContentCore.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Infrastructure.EventHandlers;

/// <summary>
/// WS4 (2026-04-17): Physical file deletion has been moved to DeleteAttachmentCommandHandler
/// which runs it AFTER SaveChanges succeeds. This ensures the file is never deleted if the
/// DB commit fails, and removes the pre-commit side-effect race condition.
///
/// This handler is intentionally a no-op. The AttachmentDeletedDomainEvent and MarkForDeletion()
/// domain method are preserved for future use (e.g., audit logging, soft-delete tracking)
/// but file I/O is no longer triggered from here.
/// </summary>
public sealed class AttachmentDeletedDomainEventHandler(
    ILogger<AttachmentDeletedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<AttachmentDeletedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<AttachmentDeletedDomainEvent> notification,
        CancellationToken ct)
    {
        logger.LogDebug(
            "AttachmentDeletedDomainEvent raised for {AttachmentId} — file deletion handled post-commit in command handler.",
            notification.Event.AttachmentId);
        return Task.CompletedTask;
    }
}